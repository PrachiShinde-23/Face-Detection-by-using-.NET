using OpenCvSharp;
using OpenCvSharp.Face;
using System.Diagnostics;

namespace FaceAttendanceSystem.Services;

/// <summary>
/// All computer-vision work lives here:
///  1. Detect faces in a frame        (Haar Cascade)
///  2. Capture training images        (webcam)
///  3. Train the recognizer           (LBPH)
///  4. Recognize a face               (LBPH predict)
/// </summary>
public class FaceEngine : IDisposable
{
    public const int FaceSize = 200;          // every face is resized to 200x200 before training/recognition
    public const double Threshold = 70.0;     // LBPH distance: LOWER = better match. Above this = "Unknown"

    private readonly CascadeClassifier _cascade;
    private readonly string _facesDir;
    private readonly string _modelPath;
    private LBPHFaceRecognizer? _recognizer;

    public FaceEngine(string cascadePath, string facesDir, string modelPath)
    {
        _cascade = new CascadeClassifier(cascadePath);
        if (_cascade.Empty())
            throw new InvalidOperationException("Face detector file could not be loaded: " + cascadePath);
        _facesDir = facesDir;
        _modelPath = modelPath;
    }

    // ---------- Step 1: Detection ----------

    /// <summary>Converts a colour frame to grayscale + equalised contrast (helps with lighting changes).</summary>
    public static Mat ToGray(Mat frame)
    {
        var gray = new Mat();
        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(gray, gray);
        return gray;
    }

    public Rect[] DetectFaces(Mat gray)
    {
        return _cascade.DetectMultiScale(
            gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new Size(100, 100));
    }

    /// <summary>Crops the face rectangle and resizes it to a fixed size.</summary>
    private static Mat CropFace(Mat gray, Rect rect)
    {
        using var roi = new Mat(gray, rect);
        var resized = new Mat();
        Cv2.Resize(roi, resized, new Size(FaceSize, FaceSize));
        return resized;
    }

    // ---------- Step 2: Capture training images ----------

    /// <summary>Opens the webcam and saves face images for one student. Returns how many were saved.</summary>
    public int CaptureFaces(int studentId, int imagesToCapture = 30)
    {
        var studentDir = Path.Combine(_facesDir, studentId.ToString());
        Directory.CreateDirectory(studentDir);

        using var capture = new VideoCapture(0);
        if (!capture.IsOpened())
        {
            Console.WriteLine("ERROR: Could not open the webcam.");
            return 0;
        }

        int saved = 0;
        var timer = Stopwatch.StartNew();
        const string window = "Registering face - look at the camera (Esc to cancel)";

        while (saved < imagesToCapture)
        {
            using var frame = new Mat();
            if (!capture.Read(frame) || frame.Empty()) break;

            using var gray = ToGray(frame);
            var faces = DetectFaces(gray);

            // Only save when exactly one face is visible, about every 200 ms (so images vary a little)
            if (faces.Length == 1 && timer.ElapsedMilliseconds > 200)
            {
                using var face = CropFace(gray, faces[0]);
                Cv2.ImWrite(Path.Combine(studentDir, $"{saved + 1}.jpg"), face);
                saved++;
                timer.Restart();
            }

            foreach (var r in faces)
                Cv2.Rectangle(frame, r, Scalar.LimeGreen, 2);
            Cv2.PutText(frame, $"Captured: {saved}/{imagesToCapture}", new Point(10, 30),
                HersheyFonts.HersheySimplex, 0.8, Scalar.Yellow, 2);

            Cv2.ImShow(window, frame);
            if (Cv2.WaitKey(30) == 27) break;   // 27 = Esc
        }

        Cv2.DestroyAllWindows();
        return saved;
    }

    // ---------- Step 3: Training ----------

    /// <summary>Reads all saved face images, trains the LBPH model and saves it to disk.</summary>
    public bool Train()
    {
        var images = new List<Mat>();
        var labels = new List<int>();

        if (!Directory.Exists(_facesDir)) return false;

        foreach (var dir in Directory.GetDirectories(_facesDir))
        {
            if (!int.TryParse(Path.GetFileName(dir), out int studentId)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.jpg"))
            {
                var img = Cv2.ImRead(file, ImreadModes.Grayscale);
                if (img.Empty()) { img.Dispose(); continue; }
                images.Add(img);
                labels.Add(studentId);   // label = student Id from the database
            }
        }

        if (images.Count == 0) return false;

        var recognizer = LBPHFaceRecognizer.Create();
        recognizer.Train(images, labels);
        recognizer.Write(_modelPath);
        _recognizer = recognizer;

        foreach (var img in images) img.Dispose();
        return true;
    }

    public bool LoadModel()
    {
        if (_recognizer != null) return true;
        if (!File.Exists(_modelPath)) return false;
        _recognizer = LBPHFaceRecognizer.Create();
        _recognizer.Read(_modelPath);
        return true;
    }

    // ---------- Step 4: Recognition ----------

    /// <summary>
    /// Predicts who a detected face is. Returns the student Id, or -1 if unknown.
    /// 'distance' is the LBPH distance (lower = more similar).
    /// </summary>
    public int Recognize(Mat gray, Rect faceRect, out double distance)
    {
        distance = double.MaxValue;
        if (_recognizer == null) return -1;

        using var face = CropFace(gray, faceRect);
        _recognizer.Predict(face, out int label, out double confidence);
        distance = confidence;
        return confidence < Threshold ? label : -1;
    }

    public void Dispose()
    {
        _cascade.Dispose();
        _recognizer?.Dispose();
    }
}

using FaceAttendanceSystem.Services;
using OpenCvSharp;

// ---------- Setup: folders and files used by the app ----------
string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
string facesDir = Path.Combine(dataDir, "faces");
Directory.CreateDirectory(facesDir);

var db = new Database(Path.Combine(dataDir, "attendance.db"));

FaceEngine engine;
try
{
    string cascadePath = CascadeProvider.EnsureCascade(dataDir);
    engine = new FaceEngine(cascadePath, facesDir, Path.Combine(dataDir, "trainer.yml"));
}
catch (Exception ex)
{
    Console.WriteLine("Startup error: " + ex.Message);
    return;
}

// ---------- Main menu ----------
while (true)
{
    Console.WriteLine();
    Console.WriteLine("=========================================");
    Console.WriteLine("     FACE ATTENDANCE SYSTEM (.NET)");
    Console.WriteLine("=========================================");
    Console.WriteLine("1. Register new student (capture face)");
    Console.WriteLine("2. Train the recognizer");
    Console.WriteLine("3. Start attendance (live camera)");
    Console.WriteLine("4. View today's attendance");
    Console.WriteLine("5. View all attendance");
    Console.WriteLine("6. Export attendance to CSV");
    Console.WriteLine("7. List registered students");
    Console.WriteLine("0. Exit");
    Console.Write("Choose an option: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": RegisterStudent(); break;
        case "2": TrainModel(); break;
        case "3": TakeAttendance(); break;
        case "4": ShowAttendance(DateTime.Now.ToString("yyyy-MM-dd")); break;
        case "5": ShowAttendance(null); break;
        case "6": ExportCsv(); break;
        case "7": ListStudents(); break;
        case "0":
            engine.Dispose();
            return;
        default:
            Console.WriteLine("Invalid option, try again.");
            break;
    }
}

// =====================================================
// Menu actions
// =====================================================

void RegisterStudent()
{
    Console.Write("Student name: ");
    string name = (Console.ReadLine() ?? "").Trim();
    Console.Write("Roll number: ");
    string roll = (Console.ReadLine() ?? "").Trim();

    if (name == "" || roll == "")
    {
        Console.WriteLine("Name and roll number are required.");
        return;
    }
    if (db.RollNoExists(roll))
    {
        Console.WriteLine("A student with this roll number already exists.");
        return;
    }

    Console.WriteLine("Camera will open. Look straight at it and slowly move your head a little.");
    int id = db.AddStudent(name, roll);
    int saved = engine.CaptureFaces(id, 30);

    if (saved < 10)
    {
        Console.WriteLine($"Only {saved} images captured - not enough. Please try again.");
        return;
    }

    Console.WriteLine($"Captured {saved} images for {name}. Training the recognizer...");
    Console.WriteLine(engine.Train() ? "Done! Student registered." : "Training failed.");
}

void TrainModel()
{
    Console.WriteLine(engine.Train()
        ? "Training complete."
        : "No face images found. Register a student first.");
}

void TakeAttendance()
{
    if (!engine.LoadModel())
    {
        Console.WriteLine("No trained model found. Register a student first (option 1) or train (option 2).");
        return;
    }

    using var capture = new VideoCapture(0);
    if (!capture.IsOpened())
    {
        Console.WriteLine("ERROR: Could not open the webcam.");
        return;
    }

    Console.WriteLine("Attendance started. Press Esc or Q in the camera window to stop.");

    const int FramesNeeded = 5;                          // same person must be seen in 5 frames in a row
    var streak = new Dictionary<int, int>();             // studentId -> consecutive frames recognised
    var markedThisSession = new List<string>();

    while (true)
    {
        using var frame = new Mat();
        if (!capture.Read(frame) || frame.Empty()) break;

        using var gray = FaceEngine.ToGray(frame);
        var faces = engine.DetectFaces(gray);
        var seenNow = new HashSet<int>();

        foreach (var rect in faces)
        {
            int id = engine.Recognize(gray, rect, out double distance);
            string label = "Unknown";
            var color = Scalar.Red;

            if (id != -1)
            {
                var student = db.GetStudent(id);
                if (student != null)
                {
                    seenNow.Add(id);
                    streak[id] = streak.TryGetValue(id, out int s) ? s + 1 : 1;
                    label = student.Name;
                    color = Scalar.LimeGreen;

                    if (streak[id] == FramesNeeded)
                    {
                        if (db.MarkAttendance(id))
                        {
                            markedThisSession.Add(student.Name);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Present: {student.Name} ({student.RollNo})");
                        }
                    }
                    if (streak[id] >= FramesNeeded) label += " - Present";
                }
            }

            Cv2.Rectangle(frame, rect, color, 2);
            Cv2.PutText(frame, label, new Point(rect.X, Math.Max(rect.Y - 10, 20)),
                HersheyFonts.HersheySimplex, 0.7, color, 2);
        }

        // reset streak for anyone not seen in this frame
        foreach (var key in streak.Keys.ToList())
            if (!seenNow.Contains(key)) streak[key] = 0;

        Cv2.PutText(frame, $"Marked this session: {markedThisSession.Count}", new Point(10, 25),
            HersheyFonts.HersheySimplex, 0.7, Scalar.Yellow, 2);
        Cv2.ImShow("Attendance - press Esc or Q to stop", frame);

        int k = Cv2.WaitKey(30);
        if (k == 27 || k == 'q' || k == 'Q') break;
    }

    Cv2.DestroyAllWindows();
    Console.WriteLine($"Attendance stopped. {markedThisSession.Count} student(s) marked present this session.");
}

void ShowAttendance(string? date)
{
    var records = db.GetAttendance(date);
    Console.WriteLine();
    Console.WriteLine(date == null ? "--- All attendance ---" : $"--- Attendance for {date} ---");
    if (records.Count == 0) { Console.WriteLine("No records found."); return; }

    Console.WriteLine($"{"Name",-20}{"Roll No",-12}{"Date",-12}{"Time",-10}");
    foreach (var r in records)
        Console.WriteLine($"{r.Name,-20}{r.RollNo,-12}{r.Date,-12}{r.Time,-10}");
}

void ExportCsv()
{
    var records = db.GetAttendance(null);
    string path = Path.Combine(dataDir, "attendance_report.csv");
    var lines = new List<string> { "Name,RollNo,Date,Time" };
    lines.AddRange(records.Select(r => $"\"{r.Name}\",\"{r.RollNo}\",{r.Date},{r.Time}"));
    File.WriteAllLines(path, lines);
    Console.WriteLine($"Exported {records.Count} record(s) to: {path}");
}

void ListStudents()
{
    var students = db.GetStudents();
    Console.WriteLine();
    if (students.Count == 0) { Console.WriteLine("No students registered yet."); return; }
    Console.WriteLine($"{"Id",-6}{"Name",-20}{"Roll No",-12}");
    foreach (var s in students)
        Console.WriteLine($"{s.Id,-6}{s.Name,-20}{s.RollNo,-12}");
}

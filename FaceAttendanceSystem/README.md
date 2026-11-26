# Face Attendance System (.NET)

A simple desktop application that **marks student attendance automatically using face recognition**.
The student just looks at the webcam - no registers, no manual entry, no proxy attendance.

Built with **C# / .NET 8**, **OpenCV (OpenCvSharp)** and **SQLite**.

---

## 1. The Problem

Manual attendance wastes class time and allows proxy attendance (one student answering for another).

## 2. The Solution

1. Register each student once by capturing their face with the webcam.
2. The system learns each student's face.
3. During class, the camera recognizes faces and marks attendance automatically - **once per student per day**.

---

## 3. Features

- Register students with their face (30 images captured automatically)
- Train a face recognition model with one click
- Live attendance through the webcam
- Shows the student's name on screen when recognized, "Unknown" for strangers
- Prevents duplicate attendance on the same day
- View today's / all attendance and export to CSV (opens in Excel)

---

## 4. Technologies Used

| Part | Technology | Why |
|---|---|---|
| Language | C# on .NET 8 | Strong, simple, widely used |
| Computer vision | OpenCvSharp4 (OpenCV for .NET) | Camera access, face detection and recognition |
| Face detection | Haar Cascade | Finds *where* the faces are in the image |
| Face recognition | LBPH (Local Binary Patterns Histograms) | Finds *who* the face belongs to |
| Database | SQLite | Lightweight, no server needed, a single file |

---

## 5. How It Works (Simple Explanation)

```
 REGISTER                         TRAIN                      ATTENDANCE
 --------                         -----                      ----------
 Webcam -> detect face            Read all saved faces       Webcam -> detect face
   -> crop + grayscale   ------>  -> LBPH learns each  ----> -> LBPH predicts "who"
   -> save 30 images              person's face pattern      -> if confident: mark present
   -> save name in database       -> save trainer.yml        -> save in database
```

**Step 1 - Face Detection (Haar Cascade):** the frame is converted to grayscale and OpenCV draws a box around every face.

**Step 2 - Face Recognition (LBPH):** for every pixel, LBPH compares it with its 8 neighbours and builds a pattern of 0s and 1s. The face is divided into a grid and a histogram of these patterns is created for each cell. These histograms are the person's "face signature". To recognize someone, the new face's signature is compared with all saved signatures and the closest one wins.

**Step 3 - Confidence check:** LBPH returns a *distance* (lower = more similar). If the distance is below the threshold (70) the person is accepted, otherwise shown as **Unknown**.

**Step 4 - Avoiding mistakes:** a student is marked present only after being recognized in **5 consecutive frames**, and the database allows only **one record per student per day**.

---

## 6. Project Structure

```
FaceAttendanceSystem/
├── Program.cs                  # Menu + main flow (register, train, attendance, reports)
├── Models/Student.cs           # Student and AttendanceRecord classes
├── Services/
│   ├── FaceEngine.cs           # Face detection, capture, training, recognition
│   ├── Database.cs             # SQLite operations
│   └── CascadeProvider.cs      # Downloads the face detector file on first run
├── FaceAttendanceSystem.csproj # Project file and NuGet packages
└── data/                       # Created automatically when you run the app
    ├── faces/<studentId>/*.jpg # Captured face images
    ├── trainer.yml             # Trained recognition model
    ├── attendance.db           # SQLite database
    └── attendance_report.csv   # Exported report
```

## 7. Database Design

**Students**: `Id` (PK), `Name`, `RollNo` (unique)
**Attendance**: `Id` (PK), `StudentId` (FK), `Date`, `Time` - with a `UNIQUE(StudentId, Date)` rule so a student can only be marked once a day.

The student `Id` is also used as the *label* for the face images, which connects the recognizer to the database.

---

## 8. How to Run

**Requirements:** Windows 10/11, a webcam, [.NET 8 SDK](https://dotnet.microsoft.com/download) (or newer), internet on the first run (to download packages and the face detector file).

```bash
git clone <your-repo-url>
cd FaceAttendanceSystem
dotnet restore
dotnet run
```

Or open the folder in **Visual Studio 2022** (File > Open > Project, choose the `.csproj`) and press **F5**.

### Using the app
1. Choose **1** - enter name and roll number, look at the camera until 30 images are captured.
2. (Training happens automatically after registering; option **2** retrains manually.)
3. Choose **3** - the camera opens; students are recognized and marked present. Press **Esc** or **Q** to stop.
4. Choose **4 / 5** to view attendance, **6** to export to CSV.

### Tips for better accuracy
- Good, even lighting on the face
- Look directly at the camera during registration, with small head movements
- Register each student in the same place where attendance will be taken

### Troubleshooting
| Problem | Fix |
|---|---|
| "Could not open the webcam" | Close other apps using the camera (Zoom, Teams); check camera privacy settings |
| Face detector download fails | Download `haarcascade_frontalface_default.xml` from the OpenCV GitHub repo and place it in the `data` folder |
| Wrong person recognized | Lower `Threshold` in `FaceEngine.cs` (e.g. 60) and re-register with more varied images |
| Real student shows "Unknown" | Raise `Threshold` (e.g. 80) or re-register in better lighting |
| Linux/macOS | Replace `OpenCvSharp4.runtime.win` in the `.csproj` with the runtime package for your OS |

---

## 9. Limitations (and Future Scope)

- LBPH is classical and sensitive to lighting and extreme angles -> upgrade to a **deep-learning model** (e.g. ArcFace / FaceNet via ONNX Runtime)
- No liveness detection (a photo of a student could fool it) -> add **blink / anti-spoofing** detection
- Console menu -> build a **WPF / WinForms / ASP.NET Core** interface with an admin dashboard
- Local SQLite -> move to **SQL Server / cloud database**, add email/SMS alerts to parents
- Add reports by date range, subject and class

## 10. Author

Your Name - your.email@example.com - GitHub: your-username

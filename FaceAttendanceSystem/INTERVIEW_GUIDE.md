# Interview Guide - Face Attendance System

Read this once before the interview. Keep it simple and speak slowly.

## 30-second pitch (memorize this)

"My project is a **Face Attendance System** built in **C# and .NET**. Instead of calling names or signing a register, the webcam recognizes the student's face and marks attendance automatically. I used **OpenCV** for face detection and recognition and **SQLite** to store the records. A student can be marked only once per day, and unknown faces are rejected."

## Explain in 4 steps

1. **Register** - webcam captures 30 face images of the student; name and roll number go to the database.
2. **Train** - the system learns each face using the **LBPH** algorithm and saves the model file (`trainer.yml`).
3. **Recognize** - live camera frames -> detect face -> predict who it is.
4. **Mark attendance** - if confident and seen in 5 frames in a row, save it to the database (once per day).

## Key terms in simple words

- **Face detection** = *where* is the face? (Haar Cascade draws a box around it)
- **Face recognition** = *whose* face is it? (LBPH compares it with saved faces)
- **Haar Cascade** = a pre-trained OpenCV file that spots face-like patterns (eyes darker than cheeks, etc.)
- **LBPH** = compares each pixel with its neighbours to make a texture pattern; the histogram of patterns is the face's "fingerprint"
- **Confidence / distance** = how different the face is from the saved one. **Lower is better.** I accept below 70.
- **Grayscale + histogram equalization** = removes colour and balances brightness so lighting matters less
- **SQLite** = a database stored in one file, no server needed

## Likely questions and answers

**Why did you choose this project?**
Manual attendance wastes time and allows proxy attendance. This automates it and is a real, practical use of computer vision.

**Why OpenCV / LBPH and not deep learning?**
LBPH is fast, works well on a normal laptop with few images per person (30), needs no GPU and is easy to explain. For higher accuracy I would upgrade to deep learning (FaceNet / ArcFace with ONNX Runtime) - that is my future scope.

**How do you stop a wrong person being marked?**
Three checks: (1) distance must be below the threshold, (2) the same person must be recognized in 5 consecutive frames, (3) the database has a UNIQUE rule on student + date.

**How do you handle unknown people?**
If the distance is above the threshold, the face is labelled "Unknown" and no attendance is saved.

**What if the same student appears twice in one day?**
The `UNIQUE(StudentId, Date)` constraint and `INSERT OR IGNORE` ignore the second entry.

**What are the limitations?**
Sensitive to lighting and angle; a printed photo could fool it (no liveness detection); console interface only.

**How would you improve it?**
Deep-learning recognition, liveness/anti-spoofing, a WPF or web dashboard, a cloud database, notifications to parents.

**How is the face linked to the student?**
The student's database `Id` is used as the label for their face images, so a prediction gives the Id and I look up the name.

**Is the data safe?**
Face images and the database stay on the local machine and are excluded from GitHub via `.gitignore`. In production I would encrypt them and take consent.

**What NuGet packages did you use?**
OpenCvSharp4, OpenCvSharp4.runtime.win, Microsoft.Data.Sqlite.

**Walk me through the code.**
- `Program.cs` - menu and main flow
- `FaceEngine.cs` - detect, capture, train, recognize
- `Database.cs` - SQL queries
- `CascadeProvider.cs` - downloads the detector file

## Live demo script (2 minutes)

1. Run `dotnet run`, show the menu.
2. Option 1 - register yourself (look at the camera, watch the counter reach 30).
3. Option 3 - show your name appear on the green box, then show "Unknown" (ask a friend or cover your face).
4. Option 4 - show today's attendance. Option 6 - export CSV and open it.
5. Say: "Recognizing again won't duplicate the entry because of the one-per-day rule."

## Be honest about

If asked something you don't know, say: "I haven't implemented that, but I would approach it by..." - panels like honesty plus a plan.

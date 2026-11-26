# Face Attendance System

## Project Overview

The **Face Attendance System** is a C# and .NET 8 based application that automatically marks student attendance using face recognition.

The system uses a webcam to capture and recognize students' faces. Once a registered student is identified, their attendance is automatically stored in an SQLite database.

The main goal of this project is to reduce manual attendance work and prevent duplicate attendance entries.

## Technologies Used

- **C#** – Main programming language.
- **.NET 8** – Development platform.
- **OpenCV (OpenCvSharp)** – Image processing and face recognition.
- **Haar Cascade** – Detects faces from webcam images.
- **LBPH Algorithm** – Recognizes registered students.
- **SQLite** – Stores student details and attendance records.

## Features

- Student registration using name and roll number.
- Captures 30 face images using a webcam.
- Face detection and recognition.
- Trains and saves the face recognition model.
- Automatically marks attendance.
- Identifies unknown faces.
- Prevents duplicate attendance on the same day.
- Displays daily and complete attendance records.
- Exports attendance reports into CSV format.

## How the Project Works

The project follows these steps:

**1. Student Registration**

The user enters the student's name and roll number. The webcam captures 30 images of the student's face and stores them.

**2. Face Detection**

The Haar Cascade algorithm detects faces from the captured images.

**3. Face Training**

The LBPH (Local Binary Patterns Histograms) algorithm learns facial patterns from the registered images. The trained model is saved as `trainer.yml`.

**4. Face Recognition**

When attendance starts, the webcam captures live video. The system detects faces and compares them with the trained model.

**5. Attendance Marking**

If a registered student is recognized in five consecutive frames, the system marks them present and saves their attendance in the database.

**6. Attendance Reports**

Users can view today's attendance, check all attendance records and export the records into a CSV file.

## Project Structure

```text
FaceAttendanceSystem/
│
├── Program.cs
├── FaceAttendanceSystem.csproj
│
├── Models/
│   └── Student.cs
│
├── Services/
│   ├── FaceEngine.cs
│   ├── Database.cs
│   └── CascadeProvider.cs
│
└── data/
    ├── faces/
    ├── trainer.yml
    ├── attendance.db
    └── attendance_report.csv
```

## Database

The project uses SQLite with two main tables:

**Students Table**
- Student ID
- Student Name
- Roll Number

**Attendance Table**
- Attendance ID
- Student ID
- Date
- Time

The database ensures that a student can be marked present only once per day.

## How to Run the Project

### Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Visual Studio or Visual Studio Code
- Webcam
- Internet connection for the first-time setup

### Installation Steps

**Step 1: Clone the Repository**

```bash
git clone <your-repository-url>
```

**Step 2: Open the Project Folder**

```bash
cd FaceAttendanceSystem
```

**Step 3: Restore Dependencies**

```bash
dotnet restore
```

**Step 4: Build the Project**

```bash
dotnet build
```

**Step 5: Run the Application**

```bash
dotnet run
```

## Application Menu

After running the application, the following options are available:

1. Register New Student
2. Train the Recognizer
3. Start Live Attendance
4. View Today's Attendance
5. View All Attendance
6. Export Attendance to CSV
7. List Registered Students
0. Exit

## Future Improvements

- Develop a graphical user interface.
- Improve face recognition accuracy using deep learning.
- Add liveness detection to prevent photo-based attendance.
- Integrate a cloud-based database.
- Generate attendance reports for different dates and classes.

## Conclusion

The Face Attendance System provides an automated way to manage student attendance using computer vision and face recognition. It combines C#, .NET 8, OpenCV and SQLite to make attendance recording easier, faster and more organized.

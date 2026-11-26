namespace FaceAttendanceSystem.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string RollNo { get; set; } = "";
}

public class AttendanceRecord
{
    public string Name { get; set; } = "";
    public string RollNo { get; set; } = "";
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
}

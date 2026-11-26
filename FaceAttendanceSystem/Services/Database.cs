using FaceAttendanceSystem.Models;
using Microsoft.Data.Sqlite;

namespace FaceAttendanceSystem.Services;

/// <summary>
/// Stores students and attendance in a local SQLite file (no server needed).
/// </summary>
public class Database
{
    private readonly string _connectionString;

    public Database(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        CreateTables();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void CreateTables()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Students (
                Id      INTEGER PRIMARY KEY AUTOINCREMENT,
                Name    TEXT NOT NULL,
                RollNo  TEXT NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS Attendance (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId  INTEGER NOT NULL,
                Date       TEXT NOT NULL,
                Time       TEXT NOT NULL,
                UNIQUE(StudentId, Date),
                FOREIGN KEY (StudentId) REFERENCES Students(Id)
            );";
        cmd.ExecuteNonQuery();
    }

    public bool RollNoExists(string rollNo)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Students WHERE RollNo = $r";
        cmd.Parameters.AddWithValue("$r", rollNo);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>Adds a student and returns the new Id (used as the face label).</summary>
    public int AddStudent(string name, string rollNo)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Students (Name, RollNo) VALUES ($n, $r); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$r", rollNo);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public Student? GetStudent(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, RollNo FROM Students WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Student { Id = reader.GetInt32(0), Name = reader.GetString(1), RollNo = reader.GetString(2) };
    }

    public List<Student> GetStudents()
    {
        var list = new List<Student>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, RollNo FROM Students ORDER BY Id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new Student { Id = reader.GetInt32(0), Name = reader.GetString(1), RollNo = reader.GetString(2) });
        return list;
    }

    /// <summary>Marks a student present for today. Returns false if already marked today.</summary>
    public bool MarkAttendance(int studentId)
    {
        var now = DateTime.Now;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Attendance (StudentId, Date, Time) VALUES ($s, $d, $t)";
        cmd.Parameters.AddWithValue("$s", studentId);
        cmd.Parameters.AddWithValue("$d", now.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$t", now.ToString("HH:mm:ss"));
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>Gets attendance records. Pass a date (yyyy-MM-dd) to filter, or null for all.</summary>
    public List<AttendanceRecord> GetAttendance(string? date = null)
    {
        var list = new List<AttendanceRecord>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT s.Name, s.RollNo, a.Date, a.Time
            FROM Attendance a
            JOIN Students s ON s.Id = a.StudentId
            WHERE ($d IS NULL OR a.Date = $d)
            ORDER BY a.Date DESC, a.Time DESC";
        cmd.Parameters.AddWithValue("$d", (object?)date ?? DBNull.Value);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AttendanceRecord
            {
                Name = reader.GetString(0),
                RollNo = reader.GetString(1),
                Date = reader.GetString(2),
                Time = reader.GetString(3)
            });
        }
        return list;
    }
}

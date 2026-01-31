using System;

[System.Serializable]
public class ErrorDetail
{
    public int detail_id;       // PK, Auto Increment
    public int log_id;          // FK (Links to the User Log)
    public int line_number;     // Line in Editor
    public string command_text; // The wrong code, e.g., "Goleff(2);"
    public string error_type;   // The type, e.g., "Misspell"
}
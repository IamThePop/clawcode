using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable] 
public class UserData
{
    public int log_id;          
    public string target_doll;  
    public int command_count;   
    public int error_count;    
    public int play_time;       
    public bool result;         
    public string created_at;   
}
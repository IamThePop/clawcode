using UnityEngine;
using UnityEngine.Networking; // Required for Internet
using System.Collections;
using System.Text; // Required for text encoding

public class APIManager : MonoBehaviour
{
    // ⚠️ IMPORTANT: Change this URL to your actual Laravel API address
    // If testing on the same computer, keep localhost.
    private string laravelAPIUrl = "http://127.0.0.1:8000/api/save-game-data";

    // Call this function to start the upload
    public void UploadUserData(string jsonPayload, string jsonDetail)
    {
        StartCoroutine(PostRequest(jsonPayload, jsonDetail));
    }

    // The Coroutine that does the actual work
    IEnumerator PostRequest(string json,string jsonDetail)
    {
        // 1. Setup the request
        var request = new UnityWebRequest(laravelAPIUrl, "POST");
        string combinedJson = "{\"summary\":" + json + ",\"errorDetails\":" + jsonDetail + "}";
        // 2. Prepare the JSON data
        Debug.Log("📤 Uploading to Laravel: " + combinedJson);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(combinedJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // 3. Set the Header so Laravel knows it is JSON
        request.SetRequestHeader("Content-Type", "application/json");

        

        // 4. Send and wait for reply
        yield return request.SendWebRequest();

        // 5. Check results
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("❌ Error uploading to Laravel: " + request.error);
        }
        else
        {
            Debug.Log("✅ Success! Server Response: " + request.downloadHandler.text);
        }
    }
}
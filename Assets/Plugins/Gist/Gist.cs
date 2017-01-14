using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Text;

public static class Gist
{
    public static IEnumerator Create(string description,
                                     Dictionary<string, string> files,
                                     Action<string> onComplete)
    {
        var fileString = new StringBuilder();

        fileString.AppendLine("{");

        bool first = true;

        foreach (var file in files)
        {
            if (!first) fileString.AppendLine(",");                        

            fileString.AppendFormat("\"{0}\": {{ \"content\": \"{1}\" }}", 
                                    file.Key, 
                                    file.Value);

            if (first) first = false;
        }

        fileString.AppendLine();
        fileString.AppendLine("}");

        var encoding = new UTF8Encoding();
        var jsonBuilder = new StringBuilder();
        jsonBuilder.Append(@"{ ""description"":""");
        jsonBuilder.Append(description);
        jsonBuilder.Append(@""", ""public"":true, ""files"": ");
        jsonBuilder.Append(fileString.ToString());
        jsonBuilder.Append(@"}");

        var request = new WWW("https://api.github.com/gists", 
                                encoding.GetBytes(jsonBuilder.ToString()));

        yield return request;

        var response = LitJson.JsonMapper.ToObject(request.text);

        onComplete((string) response["id"]);
    }

    public static IEnumerator Download(string id, 
                                       Action<Dictionary<string, string>> onComplete)
    {
        var files = new Dictionary<string, string>();
        var request = new WWW("https://api.github.com/gists/" + id);

        yield return request;

        UnityEngine.Profiling.Profiler.BeginSample("PARSE GIST");

        var response = LitJson.JsonMapper.ToObject(new LitJson.JsonReader(request.text));
        //var response = SimpleJSON.JSON.Parse(request.text);

        UnityEngine.Profiling.Profiler.EndSample();

        foreach (string file in response["files"].Keys)
        {
            files.Add(file, (string) response["files"][file]["content"]);
        }

        onComplete(files);
    }
}

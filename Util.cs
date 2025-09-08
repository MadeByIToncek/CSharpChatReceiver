using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace CSharpChatReceiver
{
    public static class Util
    {
        static readonly HttpClient Client = new();
        public static string ToJSON(Dictionary<string, object> json)
        {
            StringBuilder js = new StringBuilder();
            js.Append("{");
            foreach (string key in json.Keys)
            {
                js.Append("'").Append(key).Append("': ");
                object d = json[key];
                switch (d) {
                    case byte or char or short or int or long or float or double or bool:
                        js.Append(d);
                        break;
                    case Dictionary<string, object> objects:
                        js.Append(ToJSON(objects));
                        break;
                    default:
                        js.Append('"').Append(d.ToString()?.Replace(@"""", @"\""").Replace(@"\", @"\\")).Append('"');
                        break;
                }

                js.Append(", ");
            }

            return string.Concat(js.ToString().AsSpan(0, js.Length - 2), "}");
        }

        public static Dictionary<string, object>? ToJSON(string json)
        {
            if (!json.StartsWith("{"))
            {
                throw new ArgumentException("This is not json(map)!");
            }

            Dictionary<string, object>? result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);;
            return result;
        }

        public static Dictionary<string, object> GetJSONMap(Dictionary<string, object> json, params string[] keys)
        {
            Dictionary<string, object> map = json;
            foreach (string key in keys)
            {
                if (map.ContainsKey(key))
                {
                    map = (Dictionary<string, object>)map[key];
                }
                else
                {
                    return null;
                }
            }

            return map;
        }

        public static Dictionary<string, object>? GetJSONMap(Dictionary<string, object>? json, params object[] keys)
        {
            Dictionary<string, object>? map = json;
            IList<object> list = null;
            foreach (object key in keys)
            {
                if (map != null)
                {
                    if (map.ContainsKey(key.ToString() ?? throw new InvalidOperationException()))
                    {
                        object value = map[key.ToString() ?? throw new InvalidOperationException()];
                        if (value is List<object> objects)
                        {
                            list = objects;
                            map = null;
                        }
                        else
                        {
                            map = (Dictionary<string, object>)value;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    map = (Dictionary<string, object>)list[(int)key];
                    list = null;
                }
            }

            return map;
        }

        public static IList<object> GetJSONList(Dictionary<string, object> json, string listKey, params string[] keys)
        {
            Dictionary<string, object> map = GetJSONMap(json, keys);
            if (map != null && map.ContainsKey(listKey))
            {
                return (IList<object>)map[listKey];
            }

            return null;
        }

        public static object GetJSONValue(Dictionary<string, object> json, string key)
        {
            if (json != null && json.ContainsKey(key))
            {
                return json[key];
            }

            return null;
        }

        public static string? GetJSONValueString(Dictionary<string, object> json, string key)
        {
            object value = GetJSONValue(json, key);
            if (value != null)
            {
                return value.ToString();
            }

            return null;
        }

        public static bool GetJSONValueBoolean(Dictionary<string, object> json, string key)
        {
            object value = GetJSONValue(json, key);
            if (value != null)
            {
                return (bool)value;
            }

            return false;
        }

        public static long GetJSONValueLong(Dictionary<string, object> json, string key) {
            object value = GetJSONValue(json, key);
            if (value != null) {
                return (long)Math.Round((double)value);
            }

            return 0;
        }

        public static int GetJSONValueInt(Dictionary<string, object> json, string key)
        {
            return (int)GetJSONValueLong(json, key);
        }

        public static string GetPageContent(string url, Dictionary<string, string> header) {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            
            foreach ((string key, string value) in header) {
                requestMessage.Headers.Remove(key);
                requestMessage.Headers.Add(key,value);
            }
            requestMessage.Headers.Add("Accept-Charset", "utf-8");
            requestMessage.Headers.Add("User-Agent", YouTubeLiveChat.userAgent);

            HttpResponseMessage httpResponseMessage = Client.SendAsync(requestMessage).Result;
            return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        public static string GetPageContentWithJson(string url, string data, Dictionary<string, string> header)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            
            foreach ((string key, string value) in header) {
                requestMessage.Headers.Remove(key);
                requestMessage.Headers.Add(key,value);
            }
            requestMessage.Headers.Add("Accept-Charset", "utf-8");
            requestMessage.Headers.Add("User-Agent", YouTubeLiveChat.userAgent);
            requestMessage.Headers.Add("Content-Type","application/json");
            requestMessage.Headers.Add("Content-Length",data.Length.ToString());

            requestMessage.Content = new StringContent(data);
            
            HttpResponseMessage httpResponseMessage = Client.SendAsync(requestMessage).Result;
            return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        public static void SendHttpRequestWithJson(string url, string data, Dictionary<string, string> header)
        {
            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            
            foreach ((string key, string value) in header) {
                requestMessage.Headers.Remove(key);
                requestMessage.Headers.Add(key,value);
            }
            requestMessage.Headers.Add("Accept-Charset", "utf-8");
            requestMessage.Headers.Add("User-Agent", YouTubeLiveChat.userAgent);
            requestMessage.Headers.Add("Content-Type","application/json");
            requestMessage.Headers.Add("Content-Length",data.Length.ToString());

            requestMessage.Content = new StringContent(data);
            
            Client.SendAsync(requestMessage).Result.Dispose();
        }

        public static string GenerateClientMessageId()
        {
            string @base = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-";
            StringBuilder sb = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < 26; i++)
            {
                sb.Append(@base[random.Next(@base.Length)]);
            }

            return sb.ToString();
        }

        public static JsonElement SearchJsonElementByKey(string key, JsonProperty jsonElement)
        {
            JsonElement? value = null;

            // If input is an array, iterate through each element
            if (jsonElement.IsJsonArray())
            {
                foreach (JsonElement jsonElement1 in jsonElement.GetAsJsonArray())
                {
                    value = SearchJsonElementByKey(key, jsonElement1);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            else
            {

                // If input is object, iterate through the keys
                if (jsonElement.IsJsonObject())
                {
                    Set<Map.Entry<String, JsonElement>> entrySet = jsonElement.GetAsJsonObject().EntrySet();
                    foreach (Map.Entry<String, JsonElement> entry in entrySet)
                    {

                        // If key corresponds to the
                        string key1 = entry.GetKey();
                        if (key1.Equals(key))
                        {
                            value = entry.GetValue();
                            return value;
                        }


                        // Use the entry as input, recursively
                        value = SearchJsonElementByKey(key, entry.GetValue());
                        if (value != null)
                        {
                            return value;
                        }
                    }
                }
                else

                // If input is element, check whether it corresponds to the key
                {
                    if (jsonElement.ToString().Equals(key))
                    {
                        value = jsonElement;
                        return value;
                    }
                }
            }

            return value;
        }
    }
}
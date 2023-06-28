using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System;
using System.Net;
using System.Net.Http.Json;

public static class WebApi
{
    public static async Task<bool> Post<T>(string url, T body)
    {
        HttpClient client = new HttpClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        if(response.StatusCode == System.Net.HttpStatusCode.OK) return true;
        else return false;
    }

    public static async Task<T> PostWithResult<T, Y>(string url, Y body)
    {
        HttpClient client = new HttpClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        var a = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
        else
            return default(T);
    }

    public static async Task<T> PostFormWithResult<T>(string url, KeyValuePair<string, string>[] body)
    {
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        var content = new FormUrlEncodedContent(body);

        HttpResponseMessage response = await client.PostAsync(url, content);
        var a = response.Content.ReadAsStringAsync().Result;
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
        else
            return default(T);
    }

    public static async Task<Y> Post<T, Y>(string url, T body, string accessToken = null, string mediaType = "")
    {
        HttpClient client = new HttpClient();

        if(!string.IsNullOrEmpty(accessToken))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        HttpResponseMessage response;
        if(body.GetType() == typeof(string))
        {
            string media = string.IsNullOrEmpty(mediaType) ? "text/plain" : mediaType;
            response = await client.PostAsync(url, new StringContent(body as string, System.Text.Encoding.UTF8, media));
        }
        else
            response = await client.PostAsJsonAsync(url, body);
        
        if(response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Y>(content);
        }
        else return default(Y);
    }

    public static async Task<T> Get<T>(string url, string accessToken = null)
    {
        HttpClient client = new HttpClient();

        if(!string.IsNullOrEmpty(accessToken))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(url);
        if(response.StatusCode == System.Net.HttpStatusCode.OK) 
            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
        else 
            return default(T);
    }

    public static async Task<T> Get<T>(string url, Dictionary<string, string> headers)
    {
        HttpClient client = new HttpClient();

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
        {
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Key, header.Value);
            }

            HttpResponseMessage response = await client.SendAsync(requestMessage);
            if(response.StatusCode == System.Net.HttpStatusCode.OK) 
                return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
            else 
                return default(T);
        }
    }

    public static async Task<GETResponse<T>> GET<T>(string url, Dictionary<string, string> headers)
    {
        HttpClient client = new HttpClient();

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
        {
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Key, header.Value);
            }

            HttpResponseMessage response = await client.SendAsync(requestMessage);
            if(response.StatusCode == System.Net.HttpStatusCode.OK) 
                return new GETResponse<T>()
                {
                    data = JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result),
                    headers = response.Headers,
                    statusCode = response.StatusCode
                };
            else 
                return new GETResponse<T>()
                {
                    data = default(T),
                    headers = response.Headers,
                    statusCode = response.StatusCode
                };
        }
    }

    public static async Task<POSTResponse<T>> POST<T>(string url, object body, Dictionary<string, string> headers)
    {
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
        {
            requestMessage.Content = JsonContent.Create(body);
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"); ;//CONTENT-TYPE header
            //requestMessage.Content.Headers.Add("Content-Type", "application/json");

            foreach (var header in headers)
                requestMessage.Headers.Add(header.Key, header.Value);

            HttpResponseMessage response = await client.SendAsync(requestMessage);
            if(response.StatusCode == System.Net.HttpStatusCode.OK) 
                return new POSTResponse<T>()
                {
                    data = JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result),
                    headers = response.Headers,
                    statusCode = response.StatusCode
                };
            else 
                return new POSTResponse<T>()
                {
                    data = default(T),
                    headers = response.Headers,
                    statusCode = response.StatusCode
                };
        }
    }

    public static bool FireAndForget<T>(string url, T body)
    {
        HttpClient client = new HttpClient();

        client.PostAsJsonAsync(url, body);
        return true;
    }

    public class GETResponse<T>
    {
        public HttpStatusCode statusCode {get; set;}
        public T data {get; set;}
        public HttpResponseHeaders headers {get; set;}
    }

    public class POSTResponse<T>
    {
        public HttpStatusCode statusCode {get; set;}
        public T data {get; set;}
        public HttpResponseHeaders headers {get; set;}
    }
}
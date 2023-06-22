using HtmlAgilityPack;
using System.Net.Http.Headers;

namespace RealmeyeIdentity.Authentication
{
    public class RealmeyeService : IRealmeyeService
    {
        private readonly static ProductInfoHeaderValue UserAgent = new("Chrome", "114.0.0.0");

        public async Task<bool> ValidateCode(string name, string code)
        {
            string playerUri = GetPlayerUri(name);
            const int maxSeconds = 10;
            bool hasCode = await LookInDocument(playerUri, playerDoc =>
            {
                string descLinesXPath = $"//div[{GetContainsClassesXPath("description-line")}]";
                HtmlNodeCollection descLineNodes = playerDoc.DocumentNode.SelectNodes(descLinesXPath);

                if (descLineNodes == null)
                {
                    return false;
                }

                return descLineNodes.Any(node => node.InnerText.Contains(code));
            }, true, maxSeconds);
            return hasCode;
        }

        public Task<bool> ValidateNameChange(string oldName, string newName)
        {
            throw new NotImplementedException();
        }

        private static async Task<T?> LookInDocument<T>(
            string uri,
            Func<HtmlDocument, T> lookFunc,
            T stopResult,
            int maxSeconds)
        {
            CancellationTokenSource lookCancellation = new();
            Task lookTask = Task.Delay(TimeSpan.FromSeconds(maxSeconds), lookCancellation.Token);

            T? result = default;
            bool documentSuccess;
            do
            {
                HtmlDocument? document = await GetDocument(uri);
                documentSuccess = document != null;
                if (document != null)
                {
                    result = lookFunc.Invoke(document);
                }
            }
            while ((!documentSuccess || !Equals(result, stopResult))
                && !lookTask.IsCompleted);

            if (!lookTask.IsCompleted)
            {
                lookCancellation.Cancel();
            }

            return result;
        }

        private static async Task<HtmlDocument?> GetDocument(string uri)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.Add(UserAgent);
            HttpResponseMessage httpResponse = await client.GetAsync(uri);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return null;
            }

            string html = await httpResponse.Content.ReadAsStringAsync();
            HtmlDocument document = new();
            document.LoadHtml(html);
            return document;
        }

        private static string GetPlayerUri(string name)
        {
            const string format = "https://www.realmeye.com/player/{0}";
            return string.Format(format, name);
        }

        private static string GetNameHistoryUri(string name)
        {
            const string format = "https://www.realmeye.com/name-history-of-player/{0}";
            return string.Format(format, name);
        }

        private static string GetContainsClassesXPath(params string[] classes)
        { 
            const string format = "contains(concat(' ', normalize-space(@class), ' '), ' {0} ')";
            return string.Format(format, string.Join(" ", classes));
        }
    }
}

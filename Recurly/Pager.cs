using System;
using System.Collections;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Recurly
{
    [JsonObject]
    public class Pager<T> : IEnumerator<T>, IEnumerable<T>
    {
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }

        [JsonProperty("data")]
        public List<T> Data { get; set; }

        [JsonProperty("next")]
        public string Next { get; set; }

        internal Recurly.Client RecurlyClient { get; set; }

        private int _index = 0;

        public Pager() { }

        internal static Pager<T> Build(string url, Dictionary<string, object> queryParams, Client client)
        {
            if (queryParams != null)
            {
                url += Utils.QueryString(queryParams);
            }

            return new Pager<T>()
            {
                HasMore = true,
                Data = null,
                Next = url,
                RecurlyClient = client,
            };
        }

        public Pager<T> FetchNextPage()
        {
            var pager = RecurlyClient.MakeRequest<Pager<T>>(Method.GET, Next);
            this.Clone(pager);
            return this;
        }

        public async Task<Pager<T>> FetchNextPageAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = RecurlyClient.MakeRequestAsync<Pager<T>>(Method.GET, Next, null, null, cancellationToken);
            return await task.ContinueWith(t =>
            {
                var pager = t.Result;
                this.Clone(pager);
                return this;
            });
        }

        private void Clone(Pager<T> pager)
        {
            this.Next = pager.Next;
            this.Data = pager.Data;
            this.HasMore = pager.HasMore;
        }

        public T Current
        {
            get
            {
                return Data[_index++];
            }
        }

        object IEnumerator.Current => Current;

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            // HasMore == true on init and when the server says there are more pages of data
            // HasMore == false only when the server says this is the last page of data
            // Data == null before we've fetched any pages
            // _index >= Data.Count when we've reached the end of the current page of data
            if (HasMore && (Data == null || _index >= Data.Count))
            {
                FetchNextPage();
                _index = 0;
            }

            // _index < Data.Count when we are still iterating the current page of data
            // _index == 0 && Data.Count == 0 if the page was empty
            return _index < Data.Count;
        }

        public void Reset()
        {
            throw new NotImplementedException("Pagers cannot currently be re-used");
        }
    }
}

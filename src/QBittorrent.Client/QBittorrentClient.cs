﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QBittorrent.Client.Extensions;

namespace QBittorrent.Client
{
    public class QBittorrentClient : IDisposable
    {
        private readonly Uri _uri;
        private readonly HttpClient _client;

        public QBittorrentClient(Uri uri)
            : this(uri, new HttpClient())
        {
        }

        public QBittorrentClient(Uri uri, HttpMessageHandler handler, bool disposeHandler)
            : this(uri, new HttpClient(handler, disposeHandler))
        {
        }

        private QBittorrentClient(Uri uri, HttpClient client)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #region Authentication

        public async Task LoginAsync(string username, string password)
        {
            await _client.PostAsync(new Uri(_uri, "/login"),
                BuildForm(
                    ("username", username),
                    ("password", password)
                ))
                .ConfigureAwait(false);
        }

        public async Task LogoutAsync()
        {
            await _client.PostAsync(new Uri(_uri, "/logout"), BuildForm()).ConfigureAwait(false);
        }

        #endregion

        #region Get

        public async Task<IReadOnlyList<TorrentInfo>> GetTorrenListAsync(TorrentListQuery query = null)
        {
            query = query ?? new TorrentListQuery();
            var uri = BuildUri("/query/torrents",
                ("filter", query.Filter.ToString().ToLowerInvariant()),
                ("category", query.Category),
                ("sort", query.SortBy),
                ("reverse", query.ReverseSort.ToString().ToLowerInvariant()),
                ("limit", query.Limit?.ToString()),
                ("offset", query.Offset?.ToString()));
            var json = await _client.GetStringAsync(uri).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<TorrentInfo[]>(json);
            return result;
        }

        public async Task<TorrentProperties> GetTorrentPropertiesAsync(string hash)
        {
            var uri = BuildUri($"/query/propertiesGeneral/{hash}");
            var json = await _client.GetStringAsync(uri).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<TorrentProperties>(json);
            return result;
        }

        public async Task<IEnumerable<TorrentContent>> GetTorrentContentsAsync(string hash)
        {
            var uri = BuildUri($"/query/propertiesFiles/{hash}");
            var json = await _client.GetStringAsync(uri).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<TorrentContent[]>(json);
            return result;
        }

        #endregion

        #region Add

        public async Task AddTorrentsAsync(AddTorrentFilesRequest request)
        {
            var uri = BuildUri("/command/upload");
            var data = new MultipartFormDataContent();
            foreach (var file in request.TorrentFiles)
            {
                data.AddFile("torrents", file, "application/x-bittorrent");
            }           
            await DownloadCoreAsync(uri, data, request).ConfigureAwait(false);
        }

        public async Task AddTorrentsAsync(AddTorrentUrlsRequest request)
        {
            var uri = BuildUri("/command/download");
            var urls = string.Join("\n", request.TorrentUrls.Select(url => url.AbsoluteUri));
            var data = new MultipartFormDataContent().AddValue("urls", urls);
            await DownloadCoreAsync(uri, data, request).ConfigureAwait(false);
        }

        protected async Task DownloadCoreAsync(Uri uri, MultipartFormDataContent data, AddTorrentRequest request)
        {
            data
                .AddNonEmptyString("savepath", request.DownloadFolder)
                .AddNonEmptyString("cookie", request.Cookie)
                .AddNonEmptyString("category", request.Category)
                .AddValue("skip_checking", request.SkipHashChecking)
                .AddValue("paused", request.Paused)
                .AddNotNullValue("root_folder", request.CreateRootFolder)
                .AddNonEmptyString("rename", request.Rename)
                .AddNotNullValue("upLimit", request.UploadLimit)
                .AddNotNullValue("dlLimit", request.DownloadLimit)
                .AddValue("sequentialDownload", request.SequentialDownload)
                .AddValue("firstLastPiecePrio", request.FirstLastPiecePrioritized);

            var response = await _client.PostAsync(uri, data).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Pause/Resume

        public async Task PauseAsync(string hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            var uri = BuildUri("/command/pause");
            await _client.PostAsync(uri, BuildForm(("hash", hash))).ConfigureAwait(false);
        }

        public async Task PauseAllAsync()
        {
            var uri = BuildUri("/command/pauseAll");
            await _client.PostAsync(uri, BuildForm()).ConfigureAwait(false);
        }

        public async Task ResumeAsync(string hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            var uri = BuildUri("/command/resume");
            await _client.PostAsync(uri, BuildForm(("hash", hash))).ConfigureAwait(false);
        }

        public async Task ResumeAllAsync()
        {
            var uri = BuildUri("/command/resumeAll");
            await _client.PostAsync(uri, BuildForm()).ConfigureAwait(false);
        }

        #endregion

        #region Categories

        public async Task AddCategoryAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("The name cannot be empty.", nameof(name));

            var uri = BuildUri("/command/addCategory");
            await _client.PostAsync(uri, BuildForm(("category", name))).ConfigureAwait(false);
        }

        public async Task DeleteCategoriesAsync(params string[] names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));
            if (names.Length == 0)
                throw new ArgumentException("The name list cannot be empty.");

            var uri = BuildUri("/command/removeCategories");
            await _client.PostAsync(
                    uri,
                    BuildForm(("categories", string.Join("\n", names))))
                .ConfigureAwait(false);
        }

        public Task DeleteCategoriesAsync(IEnumerable<string> names)
        {
            return DeleteCategoriesAsync(names?.ToArray());
        }

        public async Task SetTorrentCategoryAsync(string hash, string category)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var uri = BuildUri("/command/setCategory");
            await _client.PostAsync(uri,
                BuildForm(
                    ("hashes", hash),
                    ("category", category)
                )).ConfigureAwait(false);
        }

        public async Task SetTorrentCategoryAsync(IEnumerable<string> hashes, string category)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var uri = BuildUri("/command/setCategory");
            await _client.PostAsync(uri,
                BuildForm(
                    ("hashes", string.Join("|", hashes)),
                    ("category", category)
                )).ConfigureAwait(false);
        }

        #endregion

        #region Other

        public async Task DeleteAsync(string hash, bool deleteDownloadedData = false)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            var uri = deleteDownloadedData
                ? BuildUri("/command/deletePerm")
                : BuildUri("/command/delete");
            await _client.PostAsync(uri, BuildForm(("hashes", hash))).ConfigureAwait(false);
        }

        public async Task DeleteAsync(IEnumerable<string> hashes, bool deleteDownloadedData = false)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            var uri = deleteDownloadedData
                ? BuildUri("/command/deletePerm")
                : BuildUri("/command/delete");
            await _client.PostAsync(
                uri, 
                BuildForm(("hashes", string.Join("|", hashes))))
                .ConfigureAwait(false);
        }

        public async Task SetLocationAsyc(string hash, string newLocation)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (newLocation == null)
                throw new ArgumentNullException(nameof(newLocation));

            var uri = BuildUri("/command/setLocation");
            await _client.PostAsync(uri,
                BuildForm(
                    ("hashes", hash),
                    ("location", newLocation)
                )).ConfigureAwait(false);
        }

        public async Task SetLocationAsyc(IEnumerable<string> hashes, string newLocation)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));
            if (newLocation == null)
                throw new ArgumentNullException(nameof(newLocation));

            var uri = BuildUri("/command/setLocation");
            await _client.PostAsync(uri,
                BuildForm(
                    ("hashes", string.Join("|", hashes)),
                    ("location", newLocation)
                )).ConfigureAwait(false);
        }

        public async Task RenameAsync(string hash, string newName)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (newName == null)
                throw new ArgumentNullException(nameof(newName));

            var uri = BuildUri("/command/rename");
            var response = await _client.PostAsync(uri,
                BuildForm(
                    ("hash", hash),
                    ("name", newName)
                )).ConfigureAwait(false);

            // TODO: Handle 400 response code.
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }

        private HttpContent BuildForm(params (string key, string value)[] fields)
        {
            return new CompatibleFormUrlEncodedContent(fields);
        }

        private Uri BuildUri(string path, params (string key, string value)[] parameters)
        {
            var builder = new UriBuilder(_uri)
            {
                Path = path,
                Query = string.Join("&", parameters
                    .Where(t => t.value != null)
                    .Select(t => $"{Uri.EscapeDataString(t.key)}={Uri.EscapeDataString(t.value)}"))
            };
            return builder.Uri;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using QBittorrent.Client.Extensions;

namespace QBittorrent.Client.Internal
{
    internal sealed class Api2RequestProvider : BaseRequestProvider
    {
        internal Api2RequestProvider(Uri baseUri)
        {
            Url = new Api2UrlProvider(baseUri);
        }

        public override IUrlProvider Url { get; }

        public override ApiLevel ApiLevel => ApiLevel.V2;

        public override (Uri url, HttpContent request) Pause(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Pause(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) PauseAll()
        {
            return BuildForm(Url.PauseAll(),
                ("hashes", "all"));
        }

        public override (Uri url, HttpContent request) Resume(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Resume(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) ResumeAll()
        {
            return BuildForm(Url.ResumeAll(),
                ("hashes", "all"));
        }

        public override (Uri url, HttpContent request) EditCategory(string category, string savePath)
        {
            return BuildForm(Url.EditCategory(),
                ("category", category),
                ("savePath", savePath));
        }

        public override (Uri url, HttpContent request) DeleteTorrents(IEnumerable<string> hashes, bool withFiles)
        {
            return BuildForm(Url.DeleteTorrents(withFiles),
                ("hashes", JoinHashes(hashes)),
                ("deleteFiles", withFiles.ToLowerString()));
        }

        public override (Uri url, HttpContent request) Recheck(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Recheck(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) Reannounce(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Reannounce(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent) EditTracker(string hash, Uri trackerUrl, Uri newTrackerUrl)
        {
            return BuildForm(Url.EditTracker(),
                ("hash", hash),
                ("origUrl", trackerUrl.AbsoluteUri),
                ("newUrl", newTrackerUrl.AbsoluteUri));
        }

        public override (Uri url, HttpContent) DeleteTrackers(string hash, IEnumerable<Uri> trackerUrls)
        {
            return BuildForm(Url.EditTracker(),
                ("hash", hash),
                ("urls", string.Join("|", trackerUrls.Select(u => u.AbsoluteUri))));
        }

        public override (Uri url, HttpContent request) AddTorrents(AddTorrentsRequest request)
        {
            var data = AddTorrentsCore(request);

            foreach (var file in request.TorrentFiles)
            {
                data.AddFile("torrents", file, "application/x-bittorrent");
            }

            var urls = string.Join("\n", request.TorrentUrls.Select(url => url.AbsoluteUri));
            data.AddValue("urls", urls);

            return (Url.AddTorrentFiles(), data);
        }

        public override (Uri url, HttpContent request) AddRssFolder(string path)
        {
            return BuildForm(Url.AddRssFolder(),
                ("path", path));
        }

        public override (Uri url, HttpContent request) AddRssFeed(Uri url, string path)
        {
            return BuildForm(Url.AddRssFeed(),
                ("url", url.AbsoluteUri),
                ("path", path));
        }

        public override (Uri url, HttpContent request) DeleteRssItem(string path)
        {
            return BuildForm(Url.DeleteRssItem(),
                ("path", path));
        }

        public override (Uri url, HttpContent request) MoveRssItem(string path, string destinationPath)
        {
            return BuildForm(Url.MoveRssItem(),
                ("itemPath", path),
                ("destPath", destinationPath));
        }

        public override (Uri url, HttpContent request) SetRssAutoDownloadingRule(string name, string ruleDefinition)
        {
            return BuildForm(Url.SetRssAutoDownloadingRule(),
                ("ruleName", name),
                ("ruleDef", ruleDefinition));
        }

        public override (Uri url, HttpContent request) RenameRssAutoDownloadingRule(string name, string newName)
        {
            return BuildForm(Url.RenameRssAutoDownloadingRule(),
                ("ruleName", name),
                ("newRuleName", newName));
        }

        public override (Uri url, HttpContent request) DeleteRssAutoDownloadingRule(string name)
        {
            return BuildForm(Url.DeleteRssAutoDownloadingRule(),
                ("ruleName", name));
        }
    }
}

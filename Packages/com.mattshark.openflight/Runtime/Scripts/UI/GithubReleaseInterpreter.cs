﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using TMPro;
using VRC.SDK3.Data;

namespace OpenFlightVRC
{
    public class GithubReleaseInterpreter : UdonSharpBehaviour
    {
        private VRCUrl URL = new VRCUrl("https://api.github.com/repos/Mattshark89/OpenFlight-VRC/releases?per_page=20");
        private DataDictionary[] releases;

        public string outputText = "";
        public bool onLatestRelease = false;
        public string releasesBehind = "0";
        public string latestReleaseVersion = "?.?.?";
        public OpenFlight OF;
        public AvatarListLoader AvatarListLoader;
        void Start()
        {
            //subscribe to the avatar list loader callback
            AvatarListLoader.AddCallback(this, "LoadURL");
        }

        public void LoadURL()
        {
            Logger.Log("Loading Github Releases URL...", this);
            VRCStringDownloader.LoadUrl(URL, (VRC.Udon.Common.Interfaces.IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload data)
        {
            string result = data.Result;
            Logger.Log("Loaded Github Releases URL!", this);

            //deserialize
            bool success = VRCJson.TryDeserializeFromJson(result, out DataToken json);

            //parse into releases
            CollectReleases(json);
        }

        public override void OnStringLoadError(IVRCStringDownload data)
        {
            Logger.LogError("Failed to load Github Releases URL!", this);
        }

        /// <summary>
        /// Collects all releases from a DataToken.
        /// </summary>
        /// <param name="json">The DataToken to collect releases from.</param>
        private void CollectReleases(DataToken json)
        {
            int releaseCount = json.DataList.Count;
            releases = new DataDictionary[releaseCount];
            Logger.Log(json.DataList.Count + " releases found.", this);

            for (int i = 0; i < releaseCount; i++)
            {
                json.DataList.TryGetValue(i, out DataToken release);
                releases[i] = ParseRelease(release);
            }

            outputText = "";

            //check if on latest release
            string releaseVersion = releases[0]["tag_name"].ToString();
            //remove "OpenFlight-"
            latestReleaseVersion = releaseVersion.Substring(11);
            onLatestRelease = latestReleaseVersion == OF.OpenFlightVersion.ToString();

            //check how many releases behind
            int behind = 0;
            bool foundRelease = false;

            //Format releases into output
            foreach (DataDictionary release in releases)
            {
                //stop if we've reached the release the world is on
                if (release["tag_name"].ToString() == "OpenFlight-" + OF.OpenFlightVersion.ToString())
                {
                    releasesBehind = behind.ToString();
                    foundRelease = true;
                    break;
                }

                outputText += "<b>" + release["name"].ToString() + "</b>\n";
                outputText += "Released on " + release["published_at"].ToString() + "\n";
                outputText += RemoveMarkdown(release["body"].ToString()) + "\n\n";
                behind++;
            }

            //if we didn't find the release the world is on, set the releases behind to +
            if (!foundRelease)
            {
                releasesBehind = behind.ToString() + "+";
            }

            Logger.Log("Releases behind: " + releasesBehind, this);

            //If the world is on the latest release, set the output text to say so
            if (onLatestRelease)
            {
                outputText = "You are on the latest release!";
                Logger.Log("On latest release!", this);
            }
        }

        /// <summary>
        /// Parses a release DataToken into a DataDictionary.
        /// </summary>
        /// <param name="json">The DataToken to parse.</param>
        /// <returns>The parsed DataDictionary.</returns>
        private DataDictionary ParseRelease(DataToken json)
        {
            DataDictionary releaseDict = json.DataDictionary;
            DataDictionary release = new DataDictionary();
            release.Add("tag_name", GetKeyAsString(releaseDict, "tag_name"));
            release.Add("name", GetKeyAsString(releaseDict, "name"));
            release.Add("draft", GetKeyAsString(releaseDict, "draft"));
            release.Add("prerelease", GetKeyAsString(releaseDict, "prerelease"));
            release.Add("created_at", GetKeyAsString(releaseDict, "created_at"));
            release.Add("published_at", GetKeyAsString(releaseDict, "published_at"));
            release.Add("body", GetKeyAsString(releaseDict, "body"));
            return release;
        }

        /// <summary>
        /// Gets a key from a DataDictionary as a string.
        /// </summary>
        /// <param name="dict">The DataDictionary to get the key from.</param>
        /// <param name="key">The key to get.</param>
        /// <returns>The key as a string.</returns>
        private string GetKeyAsString(DataDictionary dict, string key)
        {
            dict.TryGetValue(key, out DataToken token);
            switch (token.TokenType)
            {
                case TokenType.String:
                    return token.String;
                case TokenType.Double:
                    return token.Double.ToString();
                case TokenType.Boolean:
                    return token.Boolean.ToString();
                default:
                    return "";
            }
        }

        /// <summary>
        /// Removes all markdown from a string.
        /// </summary>
        /// <param name="markdown">The string to remove markdown from.</param>
        /// <returns>The string without markdown.</returns>
        private string RemoveMarkdown(string markdown)
        {
            //remove bold
            markdown = markdown.Replace("**", "");
            //remove italics
            markdown = markdown.Replace("*", "");
            //remove headers
            markdown = markdown.Replace("#", "");
            return markdown;
        }
    }
}

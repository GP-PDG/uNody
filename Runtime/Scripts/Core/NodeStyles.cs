using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNody
{
    public static class NodeStyles
    {
        public enum Icon
        {
            Bug,
            Exchange,
            RightArrow,
            Stop
        }

        private static string rootPath;
        private static string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(rootPath))
                    rootPath = FindRootPath("Assets");

                return rootPath;
            }
        }

        private static Dictionary<Icon, string> iconPathesByID = new();

        private static string FindRootPath(string rootPath)
        {
            var pathes = AssetDatabase.GetSubFolders(rootPath);
            foreach (var path in pathes)
            {
                if (path.Contains("uNody"))
                    return path;
                else
                {
                    var findedRootPath = FindRootPath(path);
                    if (!string.IsNullOrEmpty(findedRootPath))
                        return findedRootPath;
                }
            }

            return null;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            string iconPath = RootPath + "/Icons/";
            iconPathesByID[Icon.Bug] = iconPath + "bug.png";
            iconPathesByID[Icon.Exchange] = iconPath + "Exchange.png";
            iconPathesByID[Icon.RightArrow] = iconPath + "right-arrow.png";
            iconPathesByID[Icon.Stop] = iconPath + "stop-sign.png";
        }

        public static string GetIconPath(Icon icon)
            => iconPathesByID[icon];
    }
}

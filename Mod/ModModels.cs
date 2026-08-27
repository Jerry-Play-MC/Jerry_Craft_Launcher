using System;
using System.Collections.Generic;

namespace New_Launcher
{
    public enum ProjectType
    {
        Mod = 0,
        Modpack = 1,
        DataPack = 2,
        ResourcePack = 3,
        Shader = 4
    }

    public class ModrinthMod
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public int Downloads { get; set; }
        public string Author { get; set; }
        public string ProjectType { get; set; }
    }

    public class ModVersion
    {
        public string Id { get; set; }
        public string ProjectId { get; set; }
        public string VersionNumber { get; set; }
        public string Changelog { get; set; }
        public DateTime DatePublished { get; set; }
        public int Downloads { get; set; }
        public string VersionType { get; set; }
        public List<string> GameVersions { get; set; }
        public List<string> Loaders { get; set; }
        public bool Featured { get; set; }
        public List<ModFile> Files { get; set; }
        public string Status { get; set; }
        public string RequestedStatus { get; set; }
    }

    public class ModFile
    {
        public string Url { get; set; }
        public string Filename { get; set; }
        public bool Primary { get; set; }
        public int Size { get; set; }
        public string Sha1 { get; set; }
        public string Sha512 { get; set; }
        public List<string> GameVersions { get; set; }
        public List<string> Loaders { get; set; }
    }

    public class CurseForgeMod
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string IconUrl { get; set; }
        public int Downloads { get; set; }
    }
}
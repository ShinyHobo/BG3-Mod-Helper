﻿namespace bg3_modders_multitool.Services
{
    using LSLib.LS;
    using LSLib.LS.Enums;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Helper for reading files directly from pak files without unpacking
    /// </summary>
    public class PakReaderHelper
    {
        private PackageReader PackageReader;
        private Package Package;
        public string PakName { get; private set; }
        public List<PackagedFileInfo> PackagedFiles { get; private set; }

        public PakReaderHelper(string pakPath) {
            PackageReader = new PackageReader(pakPath);
            PakName = Path.GetFileNameWithoutExtension(pakPath);
            try
            {
                Package = PackageReader.Read();
                PackagedFiles = Package.Files.Select(f => f as PackagedFileInfo).ToList();
            }
            catch(NotAPackageException) { }
        }

        /// <summary>
        /// Reads the file from this pak and gets the contents
        /// </summary>
        /// <param name="filePath">The internal pak file path</param>
        /// <returns>The file contents</returns>
        public byte[] ReadPakFileContents(string filePath)
        {
            var file = PackagedFiles.FirstOrDefault(pf => pf.Name == filePath.Replace('\\', '/'));
            if (file == null)
                return null;

            byte[] output;
            byte[] buffer = new byte[32768];
            try
            {
                lock (file.PackageStream)
                {
                    file.PackageStream.Position = 0;
                    using (Stream ms = file.MakeStream())
                    using (BinaryReader reader = new BinaryReader(ms))
                    using (MemoryStream msStream = new MemoryStream())
                    {
                        int count;
                        while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                            msStream.Write(buffer, 0, count);
                        output = msStream.ToArray();
                    }
                }
            }
            finally
            {
                file.ReleaseStream();
            }

            return output;
        }

        /// <summary>
        /// Decompresses the selected file directly from the pak into its readable format
        /// </summary>
        /// <param name="filePath">The pak file path</param>
        public void DecompressPakFile(string filePath)
        {
            var file = PackagedFiles.FirstOrDefault(pf => pf.Name == filePath.Replace('\\', '/'));
            if (file != null)
            {
                var originalExtension = Path.GetExtension(filePath);
                var isConvertableToLsx = FileHelper.CanConvertToLsx(filePath);
                var isConvertableToXml = originalExtension.Contains("loca");
                var conversionParams = ResourceConversionParameters.FromGameVersion(Game.BaldursGate3);
                if (isConvertableToLsx)
                {
                    var newFile = filePath.Replace(originalExtension, $"{originalExtension}.lsx");
                    Resource resource = ResourceUtils.LoadResource(file.MakeStream(), ResourceUtils.ExtensionToResourceFormat(filePath));
                    ResourceUtils.SaveResource(resource, FileHelper.GetPath($"{PakName}\\{newFile}"), conversionParams);
                }
                else if (isConvertableToXml)
                {
                    var newFile = filePath.Replace(originalExtension, $"{originalExtension}.xml");
                    var resource = LocaUtils.Load(file.MakeStream(), LocaFormat.Loca);
                    LocaUtils.Save(resource, FileHelper.GetPath($"{PakName}\\{newFile}"), LocaFormat.Xml);
                }
            }
        }

        /// <summary>
        /// Gets the list of pak directory infomation
        /// </summary>
        /// <returns>The pak list</returns>
        public static List<string> GetPakList()
        {
            return Alphaleonis.Win32.Filesystem.Directory.GetFiles(FileHelper.DataDirectory, "*.pak", SearchOption.AllDirectories).Select(file => Path.GetFullPath(file)).ToList();
        }
    }
}

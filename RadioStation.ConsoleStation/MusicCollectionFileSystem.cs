using System.IO;
using System.Linq;
using RadioStation.ConsoleStation.Audio;

namespace RadioStation.ConsoleStation
{
    public class MusicCollectionFileSystem : MusicCollection
    {
        private string _path;

        public MusicCollectionFileSystem(string path)
        {
            _path = path;
        }

        public override Stream GetStream(Song song)
        {
            Stream songStream = null;

            try
            {
                using (var fileStream = File.Open(song.Id, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    songStream = new MemoryStream();
                    fileStream.CopyTo(songStream);
                    fileStream.Flush();
                    songStream.Position = 0;
                }
            }
            catch { };

            return songStream;
        }

        public override void Refresh()
        {
            string[] allSongFiles = Directory.GetFiles(_path, "*.mp3", SearchOption.AllDirectories);
            AllSongs = allSongFiles.Select(s =>
            {
                string name = Path.GetFileNameWithoutExtension(s);
                string category = Path.GetRelativePath(_path, s).Split(Path.DirectorySeparatorChar)[0];

                return new Song(s, name, category);
            }).ToArray();
        }
    }
}

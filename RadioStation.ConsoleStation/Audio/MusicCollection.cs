using System.IO;

namespace RadioStation.ConsoleStation.Audio
{
    public abstract class MusicCollection
    {
        public Song[] AllSongs { get; protected set; }

        public abstract Stream GetStream(Song song);
        public abstract void Refresh();
    }
}

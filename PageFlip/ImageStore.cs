using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
 

namespace PageFlip
{
    public class ImageStore : IDisposable
    {
        private string[] _imgPaths;
        private int _size;
        private readonly Dictionary<int, Bitmap> _cache;
        private bool _disposed;
        private int _cacheCapacity;

        public ImageStore(string _imageDir, int maxCache = 4)
        {
            _cacheCapacity = maxCache;
            if (_cacheCapacity > 0) _cache = new Dictionary<int, Bitmap>(_cacheCapacity);
            _imgPaths = EnumerateImgFiles(_imageDir);
            _size = _imgPaths.Length;
        }
        public int Length => _size;

        public Bitmap this[int index]
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(ImageStore));
                if (index < 0 || index >= _imgPaths.Length) throw new ArgumentOutOfRangeException(nameof(index));
                if (_cache != null && _cache.Count > _cacheCapacity) ClearCache();
                if (_cache.Count < _cacheCapacity && _cache != null && _cache.TryGetValue(index, out var cached)) return cached;

                var bmp = LoadBitmap(_imgPaths[index]);
                if (_cache != null && bmp != null)
                {
                    // простое кэширование; для продакшна — используйте LRU 
                    _cache[index] = bmp;
                }
                return bmp;
            }
        }

        public static PixelSize? GetFirstImageSize(string dir)
        {
            var files = EnumerateImgFiles(dir);

            if (files != null && files.Length > 0)
            {
                var bmp = LoadBitmap(files[0] ?? "");
                var sz = bmp.PixelSize;
                bmp.Dispose();
                return sz;
            }
            return null;
        }
        public void Dispose()
        {
            if (_disposed) return;
            ClearCache();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private static Bitmap LoadAvaresBitmap(string path)
        {
            try
            {
                Bitmap bmp;
                using(var single = AssetLoader.Open(new Uri(path)))
                {
                    bmp = new Bitmap(single);
                }
                return bmp;
            }
            catch { }
            return null;
        }

        private static Bitmap LoadBitmap(string path)
        {
            if (path.Contains(@"avares://"))
                return LoadAvaresBitmap(path);

            if (!File.Exists(path)) return null;
            using var s = File.OpenRead(path);
            return new Bitmap(s);
        }
        private static string[] EnumerateImgFiles(string _imageDir) 
        {
         try 
            {
                if(_imageDir.Contains(@"avares://"))
                {   
                    var asts = AssetLoader.GetAssets(new Uri(_imageDir),null); 
                    var arr = asts.Select(aa => aa.AbsoluteUri).ToArray();
                    return arr;
                }
                else
                    return Directory.EnumerateFiles(_imageDir, "*.png").Union(Directory.EnumerateFiles(_imageDir, "*.jpg")).ToArray(); 
            }           
            catch 
            { 
                           
            }
            return Array.Empty<string>();
        }
        private void ClearCache()
        {
            if (_cache != null)
            {
                foreach (var b in _cache.Values) b.Dispose();
                _cache.Clear();
            }
        }
        
    }
}

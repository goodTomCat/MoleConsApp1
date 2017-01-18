using System;
using System.IO;

namespace SharedMoleRes.Client
{
    public class UserInfo
    {
        protected string FirstNameF;
        protected string LastNameF;
        protected string PathToImageF;


        /// <exception cref="ArgumentException">Value cannot be null or empty.</exception>
        public string FirstName
        {
            get { return FirstNameF; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                FirstNameF = value;
            }
        }
        /// <exception cref="ArgumentException">Value cannot be null or empty.</exception>
        public string LastName {
            get { return LastNameF; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Value cannot be null or empty.", nameof(value))
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                LastNameF = value;
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Файла по указанному пути не существует. -or- 
        /// Расширение файла не является допустимым расширением: png, jpg.</exception>
        public string PathToImage
        {
            get { return PathToImageF; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                var info = new FileInfo(value);
                if (!info.Exists)
                    throw new ArgumentOutOfRangeException(nameof(value), "Файла по указанному пути не существует.");
                var extOfFile = info.Extension;
                if (extOfFile != "png" || extOfFile != "jpg")
                    throw new ArgumentOutOfRangeException(nameof(value), "Расширение файла не является допустимым расширением: png, jpg.");

                PathToImageF = value;
            }
        }
    }
}

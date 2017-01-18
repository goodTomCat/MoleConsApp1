using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabyLib.Other.ObjectAsDictionary;

namespace MoleClientLib.RemoteFileStream
{
    public abstract class RemoteFileStreamNaive : RemoteFileStreamBase
    {
        protected bool[] Parts;
        protected int LengthOfPart;
        protected int IndexOfPartCurrent = 0;
        protected Task<byte[]> TaskOfGetingPart;
        protected object ObjAsync = new object();
        protected byte[] PartCurrent;
        protected bool CanReadd = true;


        protected RemoteFileStreamNaive(Stream streamForTempSaving, string name) : base(streamForTempSaving, name)
        {
        }
        protected RemoteFileStreamNaive(string name, long length) : base(name, length)
        {

        }


        public override bool CanRead => CanReadd;
        public override bool CanSeek { get; } = true;
        public override bool CanWrite { get; } = false;


        /// <exception cref="ArgumentException">Длина файла превышает максимально возможную длину в 4611686014132420609 байт.</exception>
        public override Task InitializeAsync()
        {
            if (Length == 0)
            {
                LengthOfPart = 0;
                Parts = new bool[0];
                IsInitialize = true;
                return Task.CompletedTask;
            }

            long beginPartLength = 8192;
            long partsCurrent = 0;
            var partsNumb = GetBeginingPartsNumb(Length, (int) beginPartLength);
            if (Length > beginPartLength)
                while (Length/beginPartLength > partsNumb)
                    beginPartLength *= 2;

            partsCurrent = Length/beginPartLength;
            var mod = Length%beginPartLength;
            if (mod != 0)
                partsCurrent += 1;
            Parts = new bool[partsCurrent];
            IsInitialize = true;
            LengthOfPart = (int) beginPartLength;
            return Task.CompletedTask;
        }
        /// <exception cref="ArgumentNullException">buffer == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">count меньше чем 0. -or- buffer.Length меньше чем count. -or- 
        /// offset меньше чем 0.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer)) {Source = GetType().AssemblyQualifiedName};
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count)) {Source = GetType().AssemblyQualifiedName};
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset)) {Source = GetType().AssemblyQualifiedName};
            if (buffer.Length < count)
                throw new ArgumentOutOfRangeException(nameof(count)) {Source = GetType().AssemblyQualifiedName};


            if (Position >= Length)
                return 0;

            TrySetTaskOfGettingPart().Wait();
            long posNeeded = 0;
            var indexOldOfPart = IndexOfPartCurrent;
            var isInRange = Position >= (IndexOfPartCurrent + 1)*LengthOfPart - LengthOfPart &&
                            Position < (IndexOfPartCurrent + 1)*LengthOfPart;
            if (!isInRange)
            {
                var positions = GetNumbOfNeededPart(Position);
                posNeeded = positions.Item1;
                IndexOfPartCurrent = positions.Item2;
            }
            long posInPart = LengthOfPart*(1 - (IndexOfPartCurrent + 1)) + Position;
            var numbOfPartNeedeAlso = 0;
            long lengthFull = count;
            var read = LengthOfPart - posInPart;
            while (read < lengthFull && IndexOfPartCurrent + numbOfPartNeedeAlso < Parts.Length)
            {
                lengthFull -= read;
                read = LengthOfPart;
                numbOfPartNeedeAlso++;
            }

            List<byte[]> parts;
            if (indexOldOfPart == IndexOfPartCurrent && PartCurrent != null)
            {
                parts = StreamForTempSaving == null
                    ? new[] {PartCurrent}.Concat(GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, true).Result)
                        .ToList()
                    : new[] {PartCurrent}.Concat(
                        GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, true, StreamForTempSaving).Result).ToList();
            }
            else
            {
                parts = StreamForTempSaving == null
                    ? GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso).Result
                    : GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, StreamForTempSaving).Result;
            }

            var tupl = FullArrayFromOthers(parts, buffer, offset, Position, count);
            var indexEndedOfParts = tupl.Item1;
            var bytesCopyed = tupl.Item2;
            PartCurrent = parts[indexEndedOfParts];
            Position = Position + bytesCopyed;
            isInRange = Position >= (IndexOfPartCurrent + 1)*LengthOfPart - LengthOfPart &&
                        Position < (IndexOfPartCurrent + 1)*LengthOfPart;

            if (!isInRange)
                IndexOfPartCurrent += parts.Count - 1;
            return bytesCopyed;
        }
        /// <exception cref="ArgumentNullException">buffer == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">count меньше чем 0. -or- buffer.Length меньше чем count. -or- 
        /// offset меньше чем 0.</exception>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer)) {Source = GetType().AssemblyQualifiedName};
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count)) {Source = GetType().AssemblyQualifiedName};
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset)) {Source = GetType().AssemblyQualifiedName};
            if (buffer.Length < count)
                throw new ArgumentOutOfRangeException(nameof(count)) {Source = GetType().AssemblyQualifiedName};


            if (Position >= Length)
                return 0;

            await TrySetTaskOfGettingPart();
            long posNeeded = 0;
            var indexOldOfPart = IndexOfPartCurrent;
            var isInRange = Position >= (IndexOfPartCurrent + 1)*LengthOfPart - LengthOfPart &&
                            Position < (IndexOfPartCurrent + 1)*LengthOfPart;
            if (!isInRange)
            {
                var positions = GetNumbOfNeededPart(Position);
                posNeeded = positions.Item1;
                IndexOfPartCurrent = positions.Item2;
            }
            long posInPart = LengthOfPart*(1 - (IndexOfPartCurrent + 1)) + Position;
            var numbOfPartNeedeAlso = 0;
            long lengthFull = count;
            var read = LengthOfPart - posInPart;
            while (read < lengthFull && IndexOfPartCurrent + numbOfPartNeedeAlso < Parts.Length)
            {
                lengthFull -= read;
                read = LengthOfPart;
                numbOfPartNeedeAlso++;
            }

            List<byte[]> parts;
            if (indexOldOfPart == IndexOfPartCurrent && PartCurrent != null)
            {
                parts = StreamForTempSaving == null
                    ? new[] {PartCurrent}.Concat(await GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, true))
                        .ToList()
                    : new[] {PartCurrent}.Concat(
                        await GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, true, StreamForTempSaving)).ToList();
            }
            else
            {
                parts = StreamForTempSaving == null
                    ? await GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso)
                    : await GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, StreamForTempSaving);
            }

            //List<byte[]> parts = indexOldOfPart == IndexOfPartCurrent && PartCurrent != null
            //    ? new[] {PartCurrent}.Concat(await GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, posNeeded, true))
            //        .ToList()
            //    : await GetParts(IndexOfPartCurrent, numbOfPartNeedeAlso, posNeeded);

            var tupl = parts.Count*LengthOfPart > 5000
                ? await Task.Run(() => FullArrayFromOthers(parts, buffer, offset, Position, count))
                : FullArrayFromOthers(parts, buffer, offset, Position, count);
            var indexEndedOfParts = tupl.Item1;
            var bytesCopyed = tupl.Item2;
            PartCurrent = parts[indexEndedOfParts];
            Position = Position + bytesCopyed;
            isInRange = Position >= (IndexOfPartCurrent + 1)*LengthOfPart - LengthOfPart &&
                        Position < (IndexOfPartCurrent + 1)*LengthOfPart;
            //if (!isInRange)
            //    IndexOfPartCurrent += numbOfPartNeedeAlso;
            if (!isInRange)
                IndexOfPartCurrent += parts.Count - 1;

            //if (Position == Length)
            //    CanReadd = false;

            return bytesCopyed;
        }
        /// <exception cref="ArgumentOutOfRangeException">Позиция не может быть меньше нуля или больше длины файла. -or- 
        /// Если не задано временного хранилища, то позиция в потоке, используемая для поиска, должна быть 0.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (StreamForTempSaving != null)
                return StreamForTempSaving.Seek(offset, origin);
            else
            {
                if (offset < 0 || offset > Length)
                    throw new ArgumentOutOfRangeException(nameof(offset),
                        $"Позиция не может быть меньше нуля или больше длины файла. Полученное значение: {offset}.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };
                if (origin != SeekOrigin.Begin)
                    throw new ArgumentOutOfRangeException(nameof(origin),
                        "Если не задано временного хранилища, то позиция в потоке, используемая для поиска, должна быть 0.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                Positionn = offset;
                return offset;
            }

        }
        /// <exception cref="ArgumentOutOfRangeException">Длина файла не может быть меньше нуля.</exception>
        public override void SetLength(long value)
        {
            if (StreamForTempSaving != null)
                StreamForTempSaving.SetLength(value);
            else
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Длина файла не может быть меньше нуля. Полученное значение: {value}.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                Lengthh = value;
            }
        }
        /// <exception cref="Exception">Во время получения всех частей файла и перенесения их во временное хранилище, возникла ошибка.</exception>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (StreamForTempSaving == null)
                return;

            var i = -1;
            await TrySetTaskOfGettingPart().ConfigureAwait(false);
            try
            {
                for (i = 0; i < Parts.Length && !cancellationToken.IsCancellationRequested; i++)
                {
                    if (!Parts[i])
                        await GetParts(i, 0, StreamForTempSaving).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var typeOfExcSource = Type.GetType(ex.Source, false);
                if (typeOfExcSource != null)
                {
                    if (typeOfExcSource.GetTypeInfo().IsSubclassOf(typeof(RemoteFileStreamBase)) ||
                        typeOfExcSource.Equals(GetType()))
                    {
                        throw;
                    }
                }
                var str = new StringBuilder();
                str.AppendLine(
                    "Во время получения всех частей файла и перенесения их во временное хранилище, возникла ошибка.");
                str.Append($"i {i}.");
                var exc = new Exception(str.ToString(), ex);
                exc.Data.Add("StreamForTempSaving", new ObjectAsDictionary(StreamForTempSaving, "StreamForTempSaving", $"{StreamForTempSaving.GetType().Name} StreamForTempSaving"));
                throw exc;
            }
            
        }
        /// <exception cref="Exception">Во время получения всех частей файла и перенесения их во временное хранилище, возникла ошибка.</exception>
        public override void Flush()
        {
            if (StreamForTempSaving == null)
                return;

            var i = -1;
            TrySetTaskOfGettingPart().Wait();
            try
            {
                for (i = 0; i < Parts.Length; i++)
                {
                    if (!Parts[i])
                        GetParts(i, 0, StreamForTempSaving).Wait();
                }
            }
            catch (Exception ex)
            {
                var typeOfExcSource = Type.GetType(ex.Source, false);
                if (typeOfExcSource != null)
                {
                    if (typeOfExcSource.GetTypeInfo().IsSubclassOf(typeof(RemoteFileStreamBase)) ||
                        typeOfExcSource.Equals(GetType()))
                    {
                        throw;
                    }
                }
                var str = new StringBuilder();
                str.AppendLine(
                    "Во время получения всех частей файла и перенесения их во временное хранилище, возникла ошибка.");
                str.Append($"i {i}.");
                var exc = new Exception(str.ToString(), ex);
                exc.Data.Add("StreamForTempSaving", new ObjectAsDictionary(StreamForTempSaving, "StreamForTempSaving", $"{StreamForTempSaving.GetType().Name} StreamForTempSaving"));
                throw exc;
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }


        /// <exception cref="ArgumentException">Длина фалла превышает максимально возможную длину в 4611686014132420609 байт.</exception>
        protected virtual int GetBeginingPartsNumb(long fileLength, int beginingPartLength)
        {
            if (fileLength <= beginingPartLength)
                return 1;
            if (fileLength <= 100000000)
                return 100;
            if (fileLength <= 500000000)
                return 2000;
            if (fileLength <= 4611686014132420609)
                return 10000;

            if (fileLength > 4611686014132420609)
            {
                var str = new StringBuilder();
                str.AppendLine($"Длина файла превышает максимально возможную длину в 4611686014132420609 байт.");
                str.AppendLine($"fileLength: {fileLength}.");
                str.Append($"beginingPartLength: {beginingPartLength}.");
                throw new ArgumentException(str.ToString(), nameof(fileLength))
                    {Source = GetType().AssemblyQualifiedName};
            }
            return -1;
        }
        private Tuple<int, int> FullArrayFromOthers(IEnumerable<byte[]> sourses, byte[] arrayDist, int offsetInArrayDist,
            long posCurrent, int count)
        {
            //LengthOfPart *(1 - (IndexOfPartCurrent + 1)) + Position;
            long posInPart = LengthOfPart*(1 - (IndexOfPartCurrent + 1)) + posCurrent;
            var bytesCopied = 0;
            var numbOfpart = -1;
            foreach (byte[] part in sourses)
            {
                if (part.Length - posInPart + 1 <= count - bytesCopied)
                {
                    Array.Copy(part, (int)posInPart, arrayDist, bytesCopied + offsetInArrayDist,
                        (int)(part.Length - posInPart));
                    bytesCopied += (int)(part.Length - posInPart);
                }
                else
                {
                    Array.Copy(part, (int) posInPart, arrayDist, bytesCopied + offsetInArrayDist, count - bytesCopied);
                    bytesCopied += count - bytesCopied;
                    numbOfpart++;
                    break;
                }
                posInPart = 0;
                numbOfpart++;
            }
            return new Tuple<int, int>(numbOfpart, bytesCopied);
        }
        protected Task<List<byte[]>> GetParts(int posInPartsArra, int numbOfPartNeedeAlso)
        {
            return GetParts(posInPartsArra, numbOfPartNeedeAlso, false);
        }
        protected Task<List<byte[]>> GetParts(int posInPartsArra, int numbOfPartNeedeAlso, Stream fileStream)
        {
            return GetParts(posInPartsArra, numbOfPartNeedeAlso, false, fileStream);
        }
        protected virtual async Task<List<byte[]>> GetParts(int posInPartsArra, int numbOfPartNeedeAlso,
            bool second)
        {
            var parts = new List<byte[]>();
            int i = 0;
            try
            {
                if (second)
                    posInPartsArra++;
                else
                {
                    parts.Add(await GetPartAsync(LengthOfPart*posInPartsArra, LengthOfPart).ConfigureAwait(false));
                    posInPartsArra++;
                    i++;
                }
                for (int j = posInPartsArra; i < numbOfPartNeedeAlso; i++, j++)
                    parts.Add(await GetPartAsync(LengthOfPart*j, LengthOfPart).ConfigureAwait(false));
                return parts;
            }
            catch (Exception ex)
            {
                var str = new StringBuilder();
                str.AppendLine("Во время попытки получения конкретной части файла возникла ошибка.");
                str.Append($"{nameof(posInPartsArra)}: {posInPartsArra}.");
                str.AppendLine($"{nameof(i)}: {i}");
                var exc = new Exception(str.ToString(), ex) {Source = GetType().AssemblyQualifiedName};
                throw exc;
            }
        }
        protected virtual async Task<List<byte[]>> GetParts(int posInPartsArra, int numbOfPartNeedeAlso,
            bool second, Stream fileStream)
        {
            int j = -1;
            int i = 0;
            int lengthToReadOrGet;
            try
            {
                var parts = new List<byte[]>(5);
                if (second)
                    posInPartsArra++;
                else
                {
                    if (posInPartsArra == Parts.Length - 1)
                        lengthToReadOrGet = (int) (Length - LengthOfPart*posInPartsArra);
                    else
                        lengthToReadOrGet = LengthOfPart;
                    byte[] buf;
                    if (Parts[posInPartsArra])
                    {
                        buf = new byte[lengthToReadOrGet];
                        fileStream.Seek(LengthOfPart*posInPartsArra, SeekOrigin.Begin);
                        await fileStream.ReadAsync(buf, 0, buf.Length);
                    }
                    else
                    {
                        buf = await GetPartAsync(LengthOfPart*posInPartsArra, lengthToReadOrGet).ConfigureAwait(false);
                        fileStream.Seek(LengthOfPart*posInPartsArra, SeekOrigin.Begin);
                        await fileStream.WriteAsync(buf, 0, buf.Length);
                        Parts[posInPartsArra] = true;
                    }

                    parts.Add(buf);
                    posInPartsArra++;
                    i++;
                }
                var taskswriting = new List<Task>(new[] {Task.CompletedTask});
                for (j = posInPartsArra; i < numbOfPartNeedeAlso; i++, j++)
                {
                    if (j == Parts.Length - 1)
                        lengthToReadOrGet = (int) (Length - LengthOfPart*j);
                    else
                        lengthToReadOrGet = LengthOfPart;
                    byte[] buf;
                    if (Parts[j])
                    {
                        buf = new byte[lengthToReadOrGet];
                        fileStream.Seek(LengthOfPart*j, SeekOrigin.Begin);
                        await fileStream.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false);
                    }
                    else
                    {
                        buf = await GetPartAsync(LengthOfPart*j, lengthToReadOrGet).ConfigureAwait(false);
                        fileStream.Seek(LengthOfPart*j, SeekOrigin.Begin);
                        taskswriting.Add(fileStream.WriteAsync(buf, 0, buf.Length));
                        Parts[j] = true;
                    }
                    parts.Add(buf);
                }
                await Task.WhenAll(taskswriting);
                return parts;
            }
            catch (IOException ex)
            {
                throw CreateException(0, 1, ex, fileStream, posInPartsArra, j);
            }
            catch (Exception ex)
            {
                throw CreateException(0, 0, ex, posInPartsArra, numbOfPartNeedeAlso, second, j, i);
            }
            
            
        }
        protected async Task TrySetTaskOfGettingPart()
        {
            while (true)
            {
                var objOld = ObjAsync;
                var task = TaskOfGetingPart;
                if (task != null)
                    await task;

                var objNew = new object();
                if (Interlocked.CompareExchange(ref ObjAsync, objNew, objOld) == objOld)
                    return;
            }
        }
        private Tuple<int, int> GetNumbOfNeededPart(long posCurrent)
        {
            long neededPos = 0;

            long posStart = LengthOfPart;
            int posInPartsArray = 0;
            while (posCurrent >= posStart)
            {
                posStart += LengthOfPart;
                posInPartsArray++;
            }
            var posNeeded = posStart - LengthOfPart;
            return new Tuple<int, int>((int)posNeeded, posInPartsArray);
        }
        private Exception CreateException(int numb, int numbInner, params object[] objs)
        {
            var str = new StringBuilder();
            Exception result = null;
            switch (numb)
            {
                case 0:
                    switch (numbInner)
                    {
                        case 0:
                            //throw CreateException(0, 0, 0ex, 1posInPartsArra, 2numbOfPartNeedeAlso, 3second, 4j, 5i);
                            str.AppendLine(
                                "Во время попытки получения нескольких частей файла возникла непредвиденная ошибка.");
                            str.AppendLine($"posInPartsArra: {objs[1]}.");
                            str.AppendLine($"numbOfPartNeedeAlso: {objs[2]}.");
                            str.AppendLine($"second: {objs[3]}.");
                            str.AppendLine($"j: {objs[4]}.");
                            str.AppendLine($"i: {objs[5]}.");
                            result = new Exception(str.ToString(), (Exception) objs[0]);
                            break;
                        case 1:
                            //throw CreateException(0, 1, 0ex, 1fileStream, 2posInPartsArra, 3j);
                            str.AppendLine(
                                "Во время записи части файла во временный файл возникла ошибка ввода вывода.");
                            str.AppendLine($"posInPartsArra: {objs[2]}.");
                            str.AppendLine($"j: {objs[3]}.");
                            result = new IOException(str.ToString(), (Exception) objs[0]);
                            result.Data.Add("streamForTempSaving",
                                new ObjectAsDictionary(objs[1], "streamForTempSaving", $"{objs[1].GetType().Name} streamForTempSaving"));
                            break;
                    }
                    break;
                default:
                    str.AppendLine("При создании исключения не было найдено ни одного верного описания.");
                    str.AppendLine($"{nameof(numb)}: {numb}.");
                    str.AppendLine($"{nameof(numbInner)}: {numbInner}.");
                    str.Append($"{nameof(objs.Length)}: {objs.Length}.");
                    result = new Exception(str.ToString());
                    break;
            }
            return result;
        }
    }
}

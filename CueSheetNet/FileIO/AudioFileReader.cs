using CueSheetNet;
using System.Text;

namespace CueSheetNET.FileIO
{
    static internal class AudioFileReader
    {
        static public CueTime ParseDuration(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                if (ext.Contains("wav", StringComparison.OrdinalIgnoreCase))
                    return ParseWaveDuration(fs);
                else if (ext.Contains("flac", StringComparison.OrdinalIgnoreCase))
                    return ParseFlacDuration(fs);
            }
            catch (FileFormatRecognitionException)
            {
                string magic = GetBytesAsString(fs, 4, 0);
                string wave = GetBytesAsString(fs, 4, 8);
                if (magic == "RIFF" && wave == "WAVE")
                    return ParseWaveDuration(fs);
                else if (magic == "fLaC")
                    return ParseFlacDuration(fs);
            }
            throw new UnsupportedFileFormatException("Only FLAC and WAVE files are supported");
        }
        private static string GetBytesAsString(FileStream fs, int length, int offset = 0)
        {
            fs.Seek(offset, SeekOrigin.Begin);
            Span<byte> bytes = stackalloc byte[length];
            fs.Read(bytes);
            string magic = Encoding.UTF8.GetString(bytes);
            return magic;
        }
        private static CueTime ParseFlacDuration(FileStream fs)
        {
            /*
                 start_B     range_b    len  endian	type	expected
                [0     ]	[000..032]   12  big 	str		"fLaC"
                [4     ]	[032..040]   18  big 	int		header type == 0
                [5     ]	[040..064]   24  big 	int		format chunk length
                [8     ]	[064..080]   16  big 	int		Min block size
                [10    ]	[080..096]   16  big 	int		Max block size
                [12    ]	[096..120]   26  big 	int		Min frame size
                [15    ]	[120..144]   26  big 	int		Max frame size
              | [18    ]	[144..164]   20  big 	int		sample rate
              | [20.5  ]	[164..167]    3  big 	int		number of channels
              | [20.875]	[167..172]    5  big 	int		bits per sample
              | [21.5  ]	[172..208]   36  big 	str		total samples
                [26    ]	[208..336]   10  big 	int		md5
             */
            string magicBytes = GetBytesAsString(fs, 4);
            if (magicBytes != "fLaC")
                throw new FileFormatRecognitionException("The FLAC file does not begin with \"fLaC\"");
            fs.Seek(4, SeekOrigin.Begin);
            if (fs.ReadByte() != 0)
                throw new InvalidFileFormatException("Invalid FLAC metadata block header");
            Span<byte> bytes = stackalloc byte[8];
            fs.Seek(18, SeekOrigin.Begin);
            if (fs.Read(bytes) != 8)
                throw new InvalidFileFormatException("FLAC file cut short");

            // ________ ____0000 00001111 11112222
            int samples = (bytes[0] << 12)//12 insted of 16, because it spans only 2.5 bytes (20 bits)
                          + (bytes[1] << 4)
                          + (bytes[2] >> 4);
            // ________ ________ ____3333 44444444
            int totalSample_24 = (bytes[3] & 0x0F) << 16 + bytes[4];
            // ________ 55555555 66666666 77777777
            int totalSample_0 = (bytes[5] << 16)
                               + (bytes[6] << 8)
                               + bytes[7];
            // ________ ____3333 44444444 55555555 66666666 77777777
            var totalSamples = (ulong)totalSample_0 + ((ulong)totalSample_24 << 24);
            return CueTime.FromSeconds((double)totalSamples / samples);
        }
        private static CueTime ParseWaveDuration(FileStream fs)
        {
            /*
             *   range_B     range_b    endian	type	expected
                [00..04]	[000..032]  big		str		"RIFF"
                [04..08]	[032..064]  little  int		filesize-8
                [08..12]	[064..096]  big 	str		"WAVE"
                [12..16]	[096..128]  big 	str		"fmt "
                [16..20]	[128..160]  little	int		format chunk size from this point (PCM -> 16)
                [20..22]	[160..176]  little	int		format (PCM -> 1)
                [22..24]	[176..192]  little	int		num_channels
                [24..28]	[192..224]  little	int		sample_rate
                [34..36]	[272..288]  little	int		bits_per_sample
                [36..40]	[288..320]  big		str		"data"
                [40..44]	[320..336]  little	int		data_chunk_size
             */
            string magic = GetBytesAsString(fs, 4);
            string magicPlus = GetBytesAsString(fs, 4, 8);
            if (magicPlus != "WAVE" || magic != "RIFF")
                throw new FileFormatRecognitionException("The WAVE file does not begin with \"RIFF\" and \"WAVE\' after 4 bytes");
            fs.Seek(4, SeekOrigin.Begin);
            using BinaryReader binaryReader = new(fs, Encoding.Default, true);
            uint fileSize = binaryReader.ReadUInt32() + 8;
            if (fileSize != fs.Length)
                throw new InvalidFileFormatException($"Specified file length in WAVE file ({fileSize}) does not equal actual length ({fs.Length})");

            binaryReader.BaseStream.Seek(16, SeekOrigin.Begin);
            uint chunkSize = binaryReader.ReadUInt32();
            ushort format = binaryReader.ReadUInt16();
            if (format != 1)
                throw new InvalidFileFormatException($"Only PCM WAVE files with are supported (format {format})");
            if (chunkSize != 16)
                throw new InvalidFileFormatException($"Only PCM WAVE files with format chunk size of 16 are supported (chunk size: {chunkSize})");
            ushort numChannels = binaryReader.ReadUInt16();
            uint sampleRate = binaryReader.ReadUInt32();
            uint byteRate = binaryReader.ReadUInt32();
            ushort blockAlign = binaryReader.ReadUInt16();//bytes per sample (all channels)
            ushort bitsPerSample = binaryReader.ReadUInt16();
            binaryReader.BaseStream.Seek(4, SeekOrigin.Current);
            uint dataChunkSize = binaryReader.ReadUInt32();

            uint calculatedBlockAlign = (uint)numChannels * bitsPerSample / 8;
            if (blockAlign != calculatedBlockAlign)
                throw new InvalidFileFormatException($"Written BlockAlign rate does not match calculated one ({blockAlign} vs {calculatedBlockAlign})");
            uint calculatedByteRate = sampleRate * calculatedBlockAlign;
            if (byteRate != calculatedByteRate)
                throw new InvalidFileFormatException($"Written ByteRate does not match calculated one ({byteRate} vs {calculatedByteRate})");
            var numberSamples = 8D * dataChunkSize / (numChannels * bitsPerSample);
            double durationMs = numberSamples * 1000 / sampleRate;

            return CueTime.FromMilliseconds(durationMs);
        }
    }
}

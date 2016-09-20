using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlocksiteList
{
    public static class UrlUtility
    {
        public static string UrlDecode(this string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeStringFromStringInternal(str, System.Text.Encoding.UTF8);
        }
        public static string UrlDecode(string str, System.Text.Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeStringFromStringInternal(str, e);
        }

        static string UrlDecodeStringFromStringInternal(string s, System.Text.Encoding e)
        {
            int count = s.Length;
            UrlDecoder helper = new UrlDecoder(count, e);

            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes

            for (int pos = 0; pos < count; pos++)
            {
                char ch = s[pos];

                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    if (s[pos + 1] == 'u' && pos < count - 5)
                    {
                        int h1 = HexToInt(s[pos + 2]);
                        int h2 = HexToInt(s[pos + 3]);
                        int h3 = HexToInt(s[pos + 4]);
                        int h4 = HexToInt(s[pos + 5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0)
                        {   // valid 4 hex chars
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;

                            // only add as char
                            helper.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        int h1 = HexToInt(s[pos + 1]);
                        int h2 = HexToInt(s[pos + 2]);

                        if (h1 >= 0 && h2 >= 0)
                        {     // valid 2 hex chars
                            byte b = (byte)((h1 << 4) | h2);
                            pos += 2;

                            // don't add as char
                            helper.AddByte(b);
                            continue;
                        }
                    }
                }

                if ((ch & 0xFF80) == 0)
                {
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                }
                else
                {
                    helper.AddChar(ch);
                }
            }

            return helper.GetString();
        }
        static int HexToInt(char h)
        {
            return (h >= '0' && h <= '9') ? h - '0' :
            (h >= 'a' && h <= 'f') ? h - 'a' + 10 :
            (h >= 'A' && h <= 'F') ? h - 'A' + 10 :
            -1;
        }
        class UrlDecoder
        {
            int _bufferSize;

            // Accumulate characters in a special array
            int _numChars;
            char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            int _numBytes;
            byte[] _byteBuffer;

            // Encoding to convert chars to bytes
            System.Text.Encoding _encoding;

            void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal UrlDecoder(int bufferSize, System.Text.Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = new char[bufferSize];
                // byte buffer created on demand
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte(byte b)
            {
                // if there are no pending bytes treat 7 bit bytes as characters
                // this optimization is temp disable as it doesn't work for some encodings

                //if (_numBytes == 0 && ((b & 0x80) == 0)) {
                //    AddChar((char)b);
                //}
                //else

                {
                    if (_byteBuffer == null)
                    {
                        _byteBuffer = new byte[_bufferSize];
                    }

                    _byteBuffer[_numBytes++] = b;
                }
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                if (_numChars > 0)
                {
                    return new String(_charBuffer, 0, _numChars);
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}

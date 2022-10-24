using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tftp_FileTransfer.Protocol_Implementation
{
    public class TFTP
    {
        // The TFTP Protocol has 5 opcodes in the standard
        public static ushort OP_RRQ = 1; // Read Request
        public static ushort OP_WRQ = 2; // Write Request
        public static ushort OP_DATA = 3; // Data
        public static ushort OP_ACK = 4; // Acknowledgement
        public static ushort OP_ERROR = 5; // Error

        // The TFTP Protocol has 3 modes of operation in the standard
        public static string MODE_NETASCII = "netascii";
        public static string MODE_OCTET = "octet";
        public static string MODE_MAIL = "mail";

        // The TFTP Protocol has 7 defined error codes in the standard
        public static ushort ERR_UNDEFINED = 0;
        public static ushort ERR_FNF = 1; // File not Found
        public static ushort ERR_VIOL = 2; // Access Violation
        public static ushort ERR_FULL = 3; // Disk Full
        public static ushort ERR_OP = 4; // Illegal Operation
        public static ushort ERR_TID = 5; // Unknown Transfer ID
        public static ushort ERR_EXISTS = 6; // File Already Exists
        public static ushort ERR_USER = 7; // No Such User

        public static byte[] encodeRRQHeader(string fileName, string mode)
        {
            return encodeRRQWRQHeader(OP_RRQ, fileName, mode);
        }

        public static byte[] encodeWRQHeader(string fileName, string mode)
        {
            return encodeRRQWRQHeader(OP_WRQ, fileName, mode);
        }

        public static byte[] encodeRRQWRQHeader(ushort opcode, string fileName, string mode)
        {
            // Convert the two input strings into bytes for the header
            byte[] fileNameByte = Encoding.ASCII.GetBytes(fileName);
            byte[] modeByte = Encoding.ASCII.GetBytes(mode);

            // Convert the opcode into a byte array of length 2
            byte[] opcodeByte = BitConverter.GetBytes(opcode);
            Array.Reverse(opcodeByte); //We need to reverse this array because the endianness is messed up

            // Determine the number of bytes in the header (Add in the two zero bytes)
            int length = fileNameByte.Length + modeByte.Length + opcodeByte.Length + 2;

            // Create a byte array for the header
            byte[] ret = new byte[length];

            // Copy data into the return array
            int pos = 0;
            Buffer.BlockCopy(opcodeByte, 0, ret, pos, opcodeByte.Length); // Copy the opcode into the header

            pos = pos + opcodeByte.Length;
            Buffer.BlockCopy(fileNameByte, 0, ret, pos, fileNameByte.Length); // Copy the file name into the header

            pos = pos + fileNameByte.Length;
            ret[pos] = 0; // Add a zero byte to terminate string

            pos = pos + 1;
            Buffer.BlockCopy(modeByte, 0, ret, pos, modeByte.Length);

            pos = pos + modeByte.Length;
            ret[pos] = 0; // Add a zero byte to terminate string

            return ret;
        }

        public static byte[] encodeDataHeader(ushort blockNumber, byte[] data)
        {
            // Convert the opcode into a byte array of length 2
            byte[] opcodeByte = BitConverter.GetBytes(OP_DATA);
            Array.Reverse(opcodeByte); //We need to reverse this array because the endianness is messed up

            // Convert the block number into a byte array of length 2
            byte[] blockByte = BitConverter.GetBytes(blockNumber);
            Array.Reverse(blockByte); //We need to reverse this array because the endianness is messed up

            if (data.Length > 512)
            {
                throw new Exception();
            }

            int length = opcodeByte.Length + blockByte.Length + data.Length;

            byte[] ret = new byte[length];

            Buffer.BlockCopy(opcodeByte, 0, ret, 0, opcodeByte.Length);
            Buffer.BlockCopy(blockByte, 0, ret, 2, blockByte.Length);
            Buffer.BlockCopy(data, 0, ret, 4, data.Length);

            return ret;
        }

        public static byte[] encodeAckHeader(ushort blockNumber)
        {
            // Convert the opcode into a byte array of length 2
            byte[] opcodeByte = BitConverter.GetBytes(OP_ACK);
            Array.Reverse(opcodeByte); //We need to reverse this array because the endianness is messed up

            // Convert the block number into a byte array of length 2
            byte[] blockByte = BitConverter.GetBytes(blockNumber);
            Array.Reverse(blockByte); //We need to reverse this array because the endianness is messed up

            int length = opcodeByte.Length + blockByte.Length;

            byte[] ret = new byte[length];

            Buffer.BlockCopy(opcodeByte, 0, ret, 0, opcodeByte.Length);
            Buffer.BlockCopy(blockByte, 0, ret, 2, blockByte.Length);

            return ret;
        }

        public static byte[] encodeErrorHeader(ushort errorCode, string errorMessage)
        {
            // Convert the opcode into a byte array of length 2
            byte[] opcodeByte = BitConverter.GetBytes(OP_ERROR);
            Array.Reverse(opcodeByte); //We need to reverse this array because the endianness is messed up

            // Convert the error code into a byte array of length 2
            byte[] errorByte = BitConverter.GetBytes(errorCode);
            Array.Reverse(errorByte); //We need to reverse this array because the endianness is messed up

            // Convert the input string into bytes for the header
            byte[] errorMessageByte = Encoding.ASCII.GetBytes(errorMessage);

            // Determine the number of bytes in the header (Add in the zero byte)
            int length = errorByte.Length + errorMessageByte.Length + opcodeByte.Length + 1;

            byte[] ret = new byte[length];

            Buffer.BlockCopy(opcodeByte, 0, ret, 0, opcodeByte.Length);
            Buffer.BlockCopy(errorByte, 0, ret, opcodeByte.Length, errorByte.Length);
            Buffer.BlockCopy(errorMessageByte, 0, ret, opcodeByte.Length + errorByte.Length, errorMessageByte.Length);
            ret[ret.Length - 1] = 0;

            return ret;
        }

        public static int getOpcodeFromHeader(byte[] data)
        {
            // The opcode is the first and second byte of the data array
            // All opcodes in the standard only use the second byte so we return that as an int
            return data[1];
        }

        public static string getFileNameFromHeader(byte[] data)
        {
            int opcode = getOpcodeFromHeader(data);

            // You can only get a file name from the RRQ and WRQ headers
            if (opcode != OP_RRQ && opcode != OP_WRQ)
            {
                // Return null if header does not contain a file name
                return null;
            }

            // The file name is terminated with a 0 byte.
            // To grab the entire file name, we grab every byte from 2 to the 0 byte
            int pos = 0;
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i] == (byte)0)
                {
                    pos = i;
                    break;
                }
            }

            byte[] strByte = new byte[pos - 2];
            Buffer.BlockCopy(data, 2, strByte, 0, strByte.Length);

            // Convert the bytes back into a string
            return Encoding.ASCII.GetString(strByte);
        }

        public static string getModeFromHeader(byte[] data)
        {
            int opcode = getOpcodeFromHeader(data);

            // You can only get a mode from the RRQ and WRQ headers
            if (opcode != OP_RRQ && opcode != OP_WRQ)
            {
                // Return null if header does not contain a mode
                return null;
            }

            // The mode string starts after the first zero byte in the array and ends at the second
            // This loop finds the starting position based on the first zero byte
            int start = 0;
            for (int i = 2; i < data.Length; i++)
            {
                if (data[i] == (byte)0)
                {
                    start = i + 1;
                    break;
                }
            }

            // This loop finds the ending position based on the second zero byte
            int end = 0;
            for (int i = start; i < data.Length; i++)
            {
                if (data[i] == (byte)0)
                {
                    end = i;
                    break;
                }
            }

            byte[] strByte = new byte[end - start];
            Buffer.BlockCopy(data, start, strByte, 0, strByte.Length);

            // Convert the bytes back into a string
            return Encoding.ASCII.GetString(strByte);
        }

        public static int getBlockNumberFromHeader(byte[] data)
        {
            int opcode = getOpcodeFromHeader(data);

            // You can only get a block number from a DATA or an ACK header
            // Block number is also the error code in an ERROR header
            if (opcode != OP_DATA && opcode != OP_ACK && opcode != OP_ERROR)
            {
                // Return -1 if header does not contain a block number
                return -1;
            }

            // The block number is 2 bytes long in the header. We need to separate this from the header
            byte[] blockNumByte = new byte[2];
            Buffer.BlockCopy(data, 2, blockNumByte, 0, blockNumByte.Length);
            Array.Reverse(blockNumByte); // Fix endianness of this array

            return BitConverter.ToInt16(blockNumByte, 0);
        }

        public static int getErrorCodeFromHeader(byte[] data)
        {
            // The error code in the ERROR header is in the same format as
            // The block number in a DATA or ACK header. This method is a stub
            // To refer to the other method to eliminate redundant code

            return getBlockNumberFromHeader(data);
        }

        public static string getErrorMessageFromHeader(byte[] data)
        {
            int opcode = getOpcodeFromHeader(data);

            // You can only get an error message from the ERROR header
            if (opcode != OP_ERROR)
            {
                // Return null if header does not contain an error message
                return null;
            }

            // The error message is terminated with a 0 byte.
            // To grab the entire error message, we grab every byte from 4 to the 0 byte
            int pos = 0;
            for (int i = 4; i < data.Length; i++)
            {
                if (data[i] == (byte)0)
                {
                    pos = i;
                    break;
                }
            }

            byte[] strByte = new byte[pos - 4];
            Buffer.BlockCopy(data, 4, strByte, 0, strByte.Length);

            // Convert the bytes back into a string
            return Encoding.ASCII.GetString(strByte);
        }

        public static byte[] getDataFromHeader(byte[] data)
        {
            int opcode = getOpcodeFromHeader(data);

            // You can only get data from the DATA header
            if (opcode != OP_DATA)
            {
                // Return null if header does not contain data
                return null;
            }

            // The data portion of the DATA header is everything after the first 4 bytes
            byte[] ret = new byte[data.Length - 4];
            Buffer.BlockCopy(data, 4, ret, 0, ret.Length);
            return ret;
        }

        public static int getRandomPort()
        {
            // Returns a random port number for the TFTP connections to use.
            // This method does not check if ports are available.
            // This must be done by the calling program

            Random rng = new Random();

            // IANA Uses a port range from 49152 to 65535 for temporary connections
            int RNG_LB = 49152;
            int RNG_UB = 65535;

            // Return a random integer between the Lower and Upper boundaries defined above
            return rng.Next(RNG_LB, RNG_UB);
        }
    }
}


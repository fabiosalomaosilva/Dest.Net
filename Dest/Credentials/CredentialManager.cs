using System.Runtime.InteropServices;
using System.Text;

namespace Dest.Credentials;

public static class CredentialManager
{
    const int CRED_TYPE_GENERIC = 1;
    const int CRED_PERSIST_LOCAL_MACHINE = 2;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredWrite([In] ref NativeCredential userCredential, [In] uint flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool CredFree([In] IntPtr cred);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredDelete(string target, int type, int reservedFlag);


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NativeCredential
    {
        public uint Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    public static void SaveCredential(string value)
    {
        var byteArray = Encoding.Unicode.GetBytes(value);
        var credential = new NativeCredential
        {
            AttributeCount = 0,
            Attributes = IntPtr.Zero,
            Comment = IntPtr.Zero,
            TargetAlias = IntPtr.Zero,
            Type = CRED_TYPE_GENERIC,
            Persist = CRED_PERSIST_LOCAL_MACHINE,
            CredentialBlobSize = (uint)byteArray.Length,
            TargetName = Marshal.StringToCoTaskMemUni("OpenAiToken"),
            CredentialBlob = Marshal.UnsafeAddrOfPinnedArrayElement(byteArray, 0),
            UserName = Marshal.StringToCoTaskMemUni(Environment.UserName)
        };
        if (CredWrite(ref credential, 0)) return;
        var lastError = Marshal.GetLastWin32Error();
        throw new System.ComponentModel.Win32Exception(lastError);
    }

    public static string GetCredential()
    {
        if (!CredRead("OpenAiToken", CRED_TYPE_GENERIC, 0, out var credentialPtr)) return null;
        try
        {
            var credential = (NativeCredential)Marshal.PtrToStructure(credentialPtr, typeof(NativeCredential))!;
            var byteArray = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, byteArray, 0, (int)credential.CredentialBlobSize);
            return Encoding.Unicode.GetString(byteArray);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }


    public static bool DeleteCredential()
    {
        if (CredDelete("OpenAiToken", CRED_TYPE_GENERIC, 0))
        {
            return true;
        }

        var lastError = Marshal.GetLastWin32Error();
        throw new System.ComponentModel.Win32Exception(lastError);
    }


}
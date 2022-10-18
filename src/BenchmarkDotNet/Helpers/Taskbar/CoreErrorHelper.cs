﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace MS.WindowsAPICodePack.Internal
{
    /// <summary>HRESULT Wrapper</summary>
    public enum HResult
    {
        /// <summary>S_OK</summary>
        Ok = 0x0000,

        /// <summary>S_FALSE</summary>
        False = 0x0001,

        /// <summary>E_INVALIDARG</summary>
        InvalidArguments = unchecked((int)0x80070057),

        /// <summary>E_OUTOFMEMORY</summary>
        OutOfMemory = unchecked((int)0x8007000E),

        /// <summary>E_NOINTERFACE</summary>
        NoInterface = unchecked((int)0x80004002),

        /// <summary>E_FAIL</summary>
        Fail = unchecked((int)0x80004005),

        /// <summary>E_ELEMENTNOTFOUND</summary>
        ElementNotFound = unchecked((int)0x80070490),

        /// <summary>TYPE_E_ELEMENTNOTFOUND</summary>
        TypeElementNotFound = unchecked((int)0x8002802B),

        /// <summary>NO_OBJECT</summary>
        NoObject = unchecked((int)0x800401E5),

        /// <summary>Win32 Error code: ERROR_CANCELLED</summary>
        Win32ErrorCanceled = 1223,

        /// <summary>ERROR_CANCELLED</summary>
        Canceled = unchecked((int)0x800704C7),

        /// <summary>The requested resource is in use</summary>
        ResourceInUse = unchecked((int)0x800700AA),

        /// <summary>The requested resources is read-only.</summary>
        AccessDenied = unchecked((int)0x80030005)
    }

    /// <summary>Provide Error Message Helper Methods. This is intended for Library Internal use only.</summary>
    internal static class CoreErrorHelper
    {
        /// <summary>This is intended for Library Internal use only.</summary>
        public const int Ignored = (int)HResult.Ok;

        /// <summary>This is intended for Library Internal use only.</summary>
        private const int FacilityWin32 = 7;

        /// <summary>This is intended for Library Internal use only.</summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates failure.</returns>
        public static bool Failed(HResult result) => !Succeeded(result);

        /// <summary>This is intended for Library Internal use only.</summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates failure.</returns>
        public static bool Failed(int result) => !Succeeded(result);

        /// <summary>This is intended for Library Internal use only.</summary>
        /// <param name="win32ErrorCode">The Windows API error code.</param>
        /// <returns>The equivalent HRESULT.</returns>
        public static int HResultFromWin32(int win32ErrorCode)
        {
            if (win32ErrorCode > 0)
            {
                win32ErrorCode =
                    (int)(((uint)win32ErrorCode & 0x0000FFFF) | (FacilityWin32 << 16) | 0x80000000);
            }
            return win32ErrorCode;
        }

        /// <summary>This is intended for Library Internal use only.</summary>
        /// <param name="result">The COM error code.</param>
        /// <param name="win32ErrorCode">The Win32 error code.</param>
        /// <returns>Inticates that the Win32 error code corresponds to the COM error code.</returns>
        public static bool Matches(int result, int win32ErrorCode) => (result == HResultFromWin32(win32ErrorCode));

        /// <summary>This is intended for Library Internal use only.</summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates success.</returns>
        public static bool Succeeded(int result) => result >= 0;

        /// <summary>This is intended for Library Internal use only.</summary>
        /// <param name="result">The error code.</param>
        /// <returns>True if the error code indicates success.</returns>
        public static bool Succeeded(HResult result) => Succeeded((int)result);
    }
}
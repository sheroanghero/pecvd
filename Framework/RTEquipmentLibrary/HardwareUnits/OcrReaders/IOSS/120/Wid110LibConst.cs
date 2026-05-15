
#define TEST_FOR_EXTENSION_OF_LIB

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.IOSS
{
    static partial class Wid110LibConst
    {
        // ====================================================================
        /// <summary>
        /// Constants that should be used overall WID110 library software.
        /// </summary>
        // ====================================================================

        // local system resources
        //
        public static string tmpImage = "testImage_WidLib.bmp";


        // return values: these must correspond with 'WID110Dll.h'
        // these are return values only for c-function calls
        //
        // function executed sucessfully
        public const int rcNoError = 1;	// RETVAL_NO_ERROR

        // function failed, calls to FGetLastError()
        // and FGetErrorDescription() possible
        public const int rcError = 0;	// RETVAL_ERROR

        // function parameter contained invalid lib handle, call
        // to FGetErrorDescription() possible  
        public const int rcInvObj = -1;	// RETVAL_INV_OBJ


        // error codes: these must correspond with 'WID110Dll.h'
        // these are values for internal lib errors returned by FGetLastError() 
        // an error description can be retrieved by calling FGetErrorDescription()
        //
        public static int ecInvObj = -1;	// RETVAL_INV_OBJ
        public static int ecNone = 0;	// ERROR_NONE
        public static int ecNotInit = 1;	// ERROR_NOT_INIT
        public static int ecNotFound = 2;	// ERROR_READER_NOTFOUND
        public static int ecNetInit = 3;	// ERROR_NETWORK_INIT
        public static int ecNetIP = 4;	// ERROR_NETWORK_IP
        public static int ecNetSend = 5;	// ERROR_NETWORK_SEND
        public static int ecNetRecv = 6;	// ERROR_NETWORK_RECEIVE
        public static int ecFileName = 7;	// ERROR_FILENAME
        public static int ecImgSave = 8;	// ERROR_IMG_SAVE
        public static int ecParLoad = 9;	// ERROR_PARAM_LOAD
        public static int ecNoProcTrg = 10;	// ERROR_NO_PROC_TRIGGERSTR
        public static int ecArgBufSz = 11;	// ERROR_ARGBUFFERSIZE
        public static int ecNoFailSt = 12;	// ERROR_NO_FAILSTR
        public static int ecNoMoreImg = 13;	// ERROR_NO_MORE_IMAGES
        public static int ecNoResult = 14;	// ERROR_NO_VALID_RESULT
        public static int ecNoImage = 15;	// ERROR_NO_VALID_IMAGE
        public static int ecNoTrgStr = 16;	// ERROR_NO_TRIGGERSTRING
        public static int ecNoVersPar = 17;	// ERROR_VERSION_PARAMETERSET
        public static int ecPOutOfRng = 18;	// ERROR_PARAMETER_OUTOFRANGE
        public static int ecNetTrig = 19;	// ERROR_NET_TRIGGERING
        public static int ecReaderTimeout = 20;     //ERROR_READER_TIMEOUT
        public static int ecReaderNotRunning = 21;  //ERROR_READER_NOT_RUNNING


        // parameter values for FProcessGetImage(type)
        // these must correspond with 'WID110Dll.h'
        //
        public static int pvImgBest = 0;	// IMG_PROCESS_BEST
        public static int pvImgAll = 1;	// IMG_PROCESS_ALL


        // dummy results
        //
        public static string rsltFAIL = "fail";
        public static string rsltOK = "OK";
        public static string rsltERROR = "no read result";
        public static string rsltBLANK = " ";
        public static string rsltREAD = "READ: ";
        public static string rsltNOREAD = "NOREAD: ";


        // return value for FGetCodeQualityX()
        // if there is no quality retrieved, call GetLastError() then    
        public static int rsltNoCodeQuality = -1;

        // return value for FGetCodeTime()
        // if there is no time retrieved, call GetLastError() then    
        public static int rsltNoCodeTime = -1;


        // dummy error messages
        //
        public static string errNO = "no error";
        public static string errDESC = "ERROR; no error description";


        // string buffer sizes
        //
        public static int errLen = 256;

        public static int versLenCS = 63;
        public static int versLenC = versLenCS + 1;

        public static int rsltLenCS = 259;
        public static int rsltLenC = rsltLenCS + 1;
    }
}

/**********************************************************************************************//**
 * @struct	WID_CAPTURE
 *
 * @brief	WID120 Grabbing data
 *
 * @author	Jg
 * @date	10.05.2017
 **************************************************************************************************/
public struct WID_CAPTURE
{
    public int widColor;            //!< light color		(0..2)
    public int widChannel;          //!< light channel		(0..6)
    public int widIntensity;        //!< light intensity	(0..180) 0=off
    public int widRotated;          //!< Image Rotated		(0..1)
    public int widFlipped;			//!< Image Flipped		(0..1)
};

/**********************************************************************************************//**
 * @struct	WID_ROI
 *
 * @brief	A wid region of interest
 *
 * @author	Jg
 * @date	14.06.2017
 **************************************************************************************************/

public struct WID_ROI
{
    public int roiXS;                   //!< X-Start			(0..960)
    public int roiXL;                   //!< X-Length			(64..960)
    public int roiYS;                   //!< Y-Start			(0..304)
    public int roiYL;					//!< Y-Length			(64..368)
};

/**********************************************************************************************//**
 * @struct	Unsafe WID_OCR
 *
 * @brief	WID120 OCR parameter (Internal used by <see cref="Wid110LibUser.Wid110Lib"/>
 *
 * @author	Jg
 * @date	10.05.2017
 **************************************************************************************************
 *  16.04.2020   MAI    Changed char array to fixed byte. Added StructLayout attribute.
[**************************************************************************************************/
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WID_OCR_Unsafe
{
    public fixed byte widFontName[12];	    //!< Name of font				"semi_5x9","semi_org","semi_lin","ocra"
    public fixed byte widFielding[32];	    //!< Fielding of String			"aannnn-nn-cc"
    public fixed byte widFormat[32];	    //!< Fielding of String			"++++++++++++"
    public fixed byte widResult[32];	    //!< Result of String			"Reading Result"
    public int widMinSimilarity;	//!< minimum Similarity			(50..75)
    public int widAccSimilarity;	//!< accepted Similarity		(85..99)
    public int widSpacing;			//!< Spacing of Character		(80..200)
    public int widRotation;		//!< Rotation of charactor		(..90..)
    public int widAdjustSpace;		//!< Adjust Spacing				(0..1)
    public int widAdjustSize;		//!< Adjust Size of Characters	(0..1)
    public int widFilter;			//!< Filter						(0..40)
    public int widCharSizeX;		//!< Character Size in X		(15.. 60)
    public int widCharSizeY;		//!< Character Size in Y		(27..108)
    public int widCharPosX;		//!< Character Position in X	(64..960)
    public int widCharPosY;		//!< Character Position in Y	(64..304)
    public WID_ROI widRoi;				//!< Region of Interrest
};

/**********************************************************************************************//**
 * @class	WID_OCR
 *
 * @brief	WID120 OCR parameter
 *
 * @author	MAI
 * @date	16.04.2020
 **************************************************************************************************/
public class WID_OCR
{
    public string widFontName;	    //!< Name of font				"semi_5x9","semi_org","semi_lin","ocra"
    public string widFielding;	    //!< Fielding of String			"aannnn-nn-cc"
    public string widFormat;	    //!< Fielding of String			"++++++++++++"
    public string widResult;	    //!< Result of String			"Reading Result"
    public int widMinSimilarity;	//!< minimum Similarity			(50..75)
    public int widAccSimilarity;	//!< accepted Similarity		(85..99)
    public int widSpacing;			//!< Spacing of Character		(80..200)
    public int widRotation;		//!< Rotation of charactor		(..90..)
    public int widAdjustSpace;		//!< Adjust Spacing				(0..1)
    public int widAdjustSize;		//!< Adjust Size of Characters	(0..1)
    public int widFilter;			//!< Filter						(0..40)
    public int widCharSizeX;		//!< Character Size in X		(15.. 60)
    public int widCharSizeY;		//!< Character Size in Y		(27..108)
    public int widCharPosX;		//!< Character Position in X	(64..960)
    public int widCharPosY;		//!< Character Position in Y	(64..304)
    public WID_ROI widRoi;				//!< Region of Interrest

    public WID_OCR() { }
    public unsafe WID_OCR(WID_OCR_Unsafe s)
    {
        widFontName = TypeConversion.FixedByteToString(s.widFontName);
        widFielding = TypeConversion.FixedByteToString(s.widFielding);
        widFormat = TypeConversion.FixedByteToString(s.widFormat);
        widResult = TypeConversion.FixedByteToString(s.widResult);
        widMinSimilarity = s.widMinSimilarity;
        widAccSimilarity = s.widAccSimilarity;
        widSpacing = s.widSpacing;
        widRotation = s.widRotation;
        widAdjustSpace = s.widAdjustSpace;
        widAdjustSize = s.widAdjustSize;
        widFilter = s.widFilter;
        widCharSizeX = s.widCharSizeX;
        widCharSizeY = s.widCharSizeY;
        widCharPosX = s.widCharPosX;
        widCharPosY = s.widCharPosY;
        widRoi = s.widRoi;
    }

    public unsafe WID_OCR_Unsafe ToUnsafeStruct()
    {
        WID_OCR_Unsafe s = new WID_OCR_Unsafe();
        TypeConversion.StringToFixedByte(widFontName, s.widFontName, 12);
        TypeConversion.StringToFixedByte(widFielding, s.widFielding, 32);
        TypeConversion.StringToFixedByte(widFormat, s.widFormat, 32);
        TypeConversion.StringToFixedByte(widResult, s.widResult, 32);
        s.widMinSimilarity = widMinSimilarity;
        s.widAccSimilarity = widAccSimilarity;
        s.widSpacing = widSpacing;
        s.widRotation = widRotation;
        s.widAdjustSpace = widAdjustSpace;
        s.widAdjustSize = widAdjustSize;
        s.widFilter = widFilter;
        s.widCharSizeX = widCharSizeX;
        s.widCharSizeY = widCharSizeY;
        s.widCharPosX = widCharPosX;
        s.widCharPosY = widCharPosY;
        s.widRoi = widRoi;
        return s;
    }
}

// ====================================================================
/// <summary>
/// Type Conversion class.
/// </summary>
// ====================================================================
public static class TypeConversion
{
    // ====================================================================
    /// <summary>
    /// Convert from unsafe fixed byte array to managed string
    /// </summary>
    /// <param name="sender">fixed byte array</param>
    /// <returns>string</returns>
    // ====================================================================
    public static unsafe string FixedByteToString(byte* sender)
    {
        List<byte> data = new List<byte>();
        for (byte* counter = sender; *counter != 0; counter++)
        {
            data.Add(*counter);
        }
        return System.Text.Encoding.ASCII.GetString(data.ToArray()).Trim('\0');
    }

    // ====================================================================
    /// <summary>
    /// Convert from managed string to unsafe fixed byte array
    /// </summary>
    /// <param name="value">input string</param>
    /// <param name="result">fixed byte array (output)</param>
    // ====================================================================
    public static unsafe void StringToFixedByte(string value, byte* result, int length)
    {
        if (string.IsNullOrEmpty(value)) value = "";
        byte[] valueBytes = System.Text.Encoding.ASCII.GetBytes(value);
        byte* counter = result;
        for (int index = 0; index < length; index++)
        {
            if (index < value.Length) *counter = valueBytes[index];
            else *counter = 0x00;
            counter++;
        }
    }
}

/**********************************************************************************************//**
 * @struct	Unsafe WID_BCR
 *
 * @brief	WID120 Barcode Parameter (Internal used by <see cref="Wid110LibUser.Wid110Lib"/>
 *
 * @author	Jg
 * @date	10.05.2017
  **************************************************************************************************
 *  16.04.2020   MAI    Changed char array to fixed byte. Added StructLayout attribute.
**************************************************************************************************/
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WID_BCR_Unsafe
{
    public fixed byte widFormat[32];	    //!< Format of String			"++++++++++++"
    public fixed byte widConversion[48];        //!< IBM- Conversion Table		("000102030405060708091011121314151517") 
    public int widIBM_RtoL;        //!< Activate IBM BC412			(0..1)
    public int widIBM_LtoR;        //!< Activate IBM BC412			(0..1)
    public int widIBMConversion;	//!< IBM- Conversion			(0..2) None,Base 35,Custom A, Custom V, Custom R, Programmable
    public int widSemi;			    //!< Activate Semi Barcode		(0..1)
    public int widTIConversion;	    //!< TI Conversion			(0..3) None,Base 35, Custom, Custom Checksum
    public int widBarCodeLength;	//!< Code Length				(0..18) 0=auto
    public int widSeparator;        //!< Separator Character		(0..4) none,DASH,POINT,SPACE,HASH
    public int widDigits;          //!< Wafer Number				(0..3) 2 digits, suppress leading zero, 3 digits
    public int widEnableAltis;     //!< Enable Altis       		(0..1)
    public int widCheckSum;		//!< Show Check Character		(0..1)
    public int widResolution;		//!< Resolution of Barcode		(0..2) Standard, High Resolution, Standard & High Resolution
    public int widLowContrast;		//!< Contrast of Barcode		(0..1) Activate Low Contrast Codes
    public WID_ROI widRoi;				//!< Region of Interrest
};

/**********************************************************************************************//**
 * @class	WID_BCR
 *
 * @brief	WID120 Barcode parameter
 *
 * @author	MAI
 * @date	16.04.2020
 **************************************************************************************************/
public class WID_BCR
{
    public string widFormat;	    //!< Format of String			"++++++++++++"
    public string widConversion;	    //!< IBM- Conversion Table		("000102030405060708091011121314151517") 
    public int widIBM_RtoL;        //!< Activate IBM BC412			(0..1)
    public int widIBM_LtoR;        //!< Activate IBM BC412			(0..1)
    public int widIBMConversion;	//!< IBM- Conversion			(0..2) None,Base 35,Custom A, Custom V, Custom R, Programmable
    public int widSemi;			    //!< Activate Semi Barcode		(0..1)
    public int widTIConversion;	    //!< TI Conversion			(0..3) None,Base 35, Custom, Custom Checksum
    public int widBarCodeLength;	//!< Code Length				(0..18) 0=auto
    public int widSeparator;        //!< Separator Character		(0..4) none,DASH,POINT,SPACE,HASH
    public int widDigits;          //!< Wafer Number				(0..3) 2 digits, suppress leading zero, 3 digits
    public int widEnableAltis;     //!< Enable Altis       		(0..1)
    public int widCheckSum;		//!< Show Check Character		(0..1)
    public int widResolution;		//!< Resolution of Barcode		(0..2) Standard, High Resolution, Standard & High Resolution
    public int widLowContrast;		//!< Contrast of Barcode		(0..1) Activate Low Contrast Codes
    public WID_ROI widRoi;				//!< Region of Interrest

    public WID_BCR() { }
    public unsafe WID_BCR(WID_BCR_Unsafe s)
    {
        widFormat = TypeConversion.FixedByteToString(s.widFormat);
        widConversion = TypeConversion.FixedByteToString(s.widConversion);
        widIBM_RtoL = s.widIBM_RtoL;
        widIBM_LtoR = s.widIBM_LtoR;
        widIBMConversion = s.widIBMConversion;
        widSemi = s.widSemi;
        widTIConversion = s.widTIConversion;
        widBarCodeLength = s.widBarCodeLength;
        widSeparator = s.widSeparator;
        widDigits = s.widDigits;
        widEnableAltis = s.widEnableAltis;
        widCheckSum = s.widCheckSum;
        widResolution = s.widResolution;
        widLowContrast = s.widLowContrast;
        widRoi = s.widRoi;
    }

    public unsafe WID_BCR_Unsafe ToUnsafeStruct()
    {
        WID_BCR_Unsafe s = new WID_BCR_Unsafe();
        TypeConversion.StringToFixedByte(widFormat, s.widFormat, 32);
        TypeConversion.StringToFixedByte(widConversion, s.widConversion, 48);
        s.widIBM_RtoL = widIBM_RtoL;
        s.widIBM_LtoR = widIBM_LtoR;
        s.widIBMConversion = widIBMConversion;
        s.widSemi = widSemi;
        s.widTIConversion = widTIConversion;
        s.widBarCodeLength = widBarCodeLength;
        s.widSeparator = widSeparator;
        s.widDigits = widDigits;
        s.widEnableAltis = widEnableAltis;
        s.widCheckSum = widCheckSum;
        s.widResolution = widResolution;
        s.widLowContrast = widLowContrast;
        s.widRoi = widRoi;
        return s;
    }
}


/**********************************************************************************************//**
 * @struct	Unsafe WID_DMR
 *
 * @brief	WID120 2D-Code Parameter (Internal used by <see cref="Wid110LibUser.Wid110Lib"/>
 *
 * @author	Jg
 * @date	10.05.2017
  **************************************************************************************************
 *  16.04.2020   MAI    Changed char array to fixed byte. Added StructLayout attribute.
**************************************************************************************************/
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WID_DMR_Unsafe
{
    public int widSymbol;          //!< Activate Code Type			(0 = DataMatrix, 1 = QR-Code
    public int widLinNrm;          //!< linear Code (normal)		(0/1)	off/on
    public int widLinInv;          //!< linear Code (invers)		(0/1)	off/on
    public int widDotNrm;          //!< Punkt  Code (normal)		(0/1)	off/on
    public int widDotInv;          //!< Punkt Code (invers)		(0/1)	off/on
    public int widCodeType;		//!< Activate Code Type			(0/1)	normal/mirrored
    public int widCodeSize;		//!< Module Size of Code		(0..3)	large, semi, small, tiny
    public fixed byte widFormat[32];		//!< Format of String			"++++++++++++"
    public WID_ROI widRoi;				//!< Region of Interrest
};

/**********************************************************************************************//**
 * @class	WID_DMR
 *
 * @brief	WID120 2D-Code parameter
 *
 * @author	MAI
 * @date	16.04.2020
 **************************************************************************************************/
public class WID_DMR
{
    public int widSymbol;          //!< Activate Code Type			(0 = DataMatrix, 1 = QR-Code
    public int widLinNrm;          //!< linear Code (normal)		(0/1)	off/on
    public int widLinInv;          //!< linear Code (invers)		(0/1)	off/on
    public int widDotNrm;          //!< Punkt  Code (normal)		(0/1)	off/on
    public int widDotInv;          //!< Punkt Code (invers)		(0/1)	off/on
    public int widCodeType;		//!< Activate Code Type			(0/1)	normal/mirrored
    public int widCodeSize;		//!< Module Size of Code		(0..3)	large, semi, small, tiny
    public string widFormat;		//!< Format of String			"++++++++++++"
    public WID_ROI widRoi;				//!< Region of Interrest

    public WID_DMR() { }
    public unsafe WID_DMR(WID_DMR_Unsafe s)
    {
        widSymbol = s.widSymbol;
        widLinNrm = s.widLinNrm;
        widLinInv = s.widLinInv;
        widDotNrm = s.widDotNrm;
        widDotInv = s.widDotInv;
        widCodeType = s.widCodeType;
        widCodeSize = s.widCodeSize;
        widFormat = TypeConversion.FixedByteToString(s.widFormat);
        widRoi = s.widRoi;
    }

    public unsafe WID_DMR_Unsafe ToUnsafeStruct()
    {
        WID_DMR_Unsafe s = new WID_DMR_Unsafe();
        s.widSymbol = widSymbol;
        s.widLinNrm = widLinNrm;
        s.widLinInv = widLinInv;
        s.widDotNrm = widDotNrm;
        s.widDotInv = widDotInv;
        s.widCodeType = widCodeType;
        s.widCodeSize = widCodeSize;
        TypeConversion.StringToFixedByte(widFormat, s.widFormat, 32);
        s.widRoi = widRoi;
        return s;
    }
}


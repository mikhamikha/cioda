//-----------------------------------------------------------------------------
//  Copyright (c) 2004 Citect Pty Limited.
//  All rights reserved. 
//-----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Citect.Util
{
	/// <summary>
	///		C# interface to CTAPI.
	/// </summary>
	public class CTAPI
	{
		#region Fields ---------------------------------------------------------------
		/// <summary>
		/// use encryption
		/// </summary>
		public const uint CT_OPEN_CRYPT	= 0x00000001;

		/// <summary>
		/// reconnect on failure
		/// </summary>
		public const uint CT_OPEN_RECONNECT = 0x00000002;

		/// <summary>
		/// read only mode
		/// </summary>
		public const uint CT_OPEN_READ_ONLY = 0x00000004;

		/// <summary>
		/// batch mode
		/// </summary>
		public const uint CT_OPEN_BATCH = 0x00000008;

		/// <summary>
		/// scroll to next record
		/// </summary>
		public const int CT_FIND_SCROLL_NEXT = 0x00000001;

		/// <summary>
		/// scroll to prev record
		/// </summary>
		public const int CT_FIND_SCROLL_PREV = 0x00000002;	

		/// <summary>
		/// scroll to first record
		/// </summary>
		public const int CT_FIND_SCROLL_FIRST = 0x00000003;	

		/// <summary>
		/// scroll to last record
		/// </summary>
		public const int CT_FIND_SCROLL_LAST = 0x00000004;

		/// <summary>
		/// scroll to absolute record
		/// </summary>
		public const int CT_FIND_SCROLL_ABSOLUTE = 0x00000005;	

		/// <summary>
		/// scroll to relative record
		/// </summary>
		public const int CT_FIND_SCROLL_RELATIVE =0x00000006;	

		/// <summary>
		/// list event mode
		/// </summary>
		public const int CT_LIST_EVENT = 0x00000001;

		/// <summary>
		/// get event for new tags
		/// </summary>
		public const int CT_LIST_EVENT_NEW = 0x00000001;

		/// <summary>
		/// get events for status change
		/// </summary>
		public const int CT_LIST_EVENT_STATUS = 0x00000002;

		/// <summary>
		/// don't scale the variable
		/// </summary>
		public const int CT_FMT_NO_SCALE = 0x00000001;

		/// <summary>
		/// don't apply format
		/// </summary>
		public const int CT_FMT_NO_FORMAT = 0x00000002;

		/// <summary>
		/// get last value of data
		/// </summary>
		public const int CT_FMT_LAST = 0x00000004;

		/// <summary>
		/// range check the variable
		/// </summary>
		public const int CT_FMT_RANGE_CHECK = 0x00000008;

		public enum DBTYPEENUM 
		{
			DBTYPE_EMPTY		= 0,
			DBTYPE_NULL		= 1,
			DBTYPE_I2		= 2,
			DBTYPE_I4		= 3,
			DBTYPE_R4		= 4,
			DBTYPE_R8		= 5,
			DBTYPE_CY		= 6,
			DBTYPE_DATE		= 7,
			DBTYPE_BSTR		= 8,
			DBTYPE_IDISPATCH	= 9,
			DBTYPE_ERROR		= 10,
			DBTYPE_BOOL		= 11,
			DBTYPE_VARIANT		= 12,
			DBTYPE_IUNKNOWN		= 13,
			DBTYPE_DECIMAL		= 14,
			DBTYPE_UI1		= 17,
			DBTYPE_ARRAY		= 0x2000,
			DBTYPE_BYREF		= 0x4000,
			DBTYPE_I1		= 16,
			DBTYPE_UI2		= 18,
			DBTYPE_UI4		= 19,
			DBTYPE_I8		= 20,
			DBTYPE_UI8		= 21,
			DBTYPE_GUID		= 72,
			DBTYPE_VECTOR		= 0x1000,
			DBTYPE_RESERVED		= 0x8000,
			DBTYPE_BYTES		= 128,
			DBTYPE_STR		= 129,
			DBTYPE_WSTR		= 130,
			DBTYPE_NUMERIC		= 131,
			DBTYPE_UDT		= 132,
			DBTYPE_DBDATE		= 133,
			DBTYPE_DBTIME		= 134,
			DBTYPE_DBTIMESTAMP	= 135
		};

		#endregion // Fields

		#region Constructors ---------------------------------------------------------

		/// <summary>
		///		Initializes a new instance of the CTAPI class.
		/// </summary>
		public CTAPI()
		{
		}

		#endregion // Constructors

		#region Events ---------------------------------------------------------------
		#endregion // Events Handlers

		#region Properties -----------------------------------------------------------
		#endregion // Properties

		#region Public Methods -------------------------------------------------------

		/// <summary>
		/// Opens a connection to the CitectHMI/SCADA API.  
		/// The CTAPI.DLL is initialised and a connection is made to CitectHMI/SCADA.  
		/// If CitectHMI/SCADA is not running when this function is called, 
		/// the function will fail.  This function must be called before any other 
		/// CTAPI function to initialise the connection to CitectHMI/SCADA.
		/// If you use the CT_OPEN_RECONNECT mode, and the connection is lost, 
		/// the CTAPI will attempt to reconnect to CitectHMI/SCADA.  
		/// When the connection has been re-established, you can continue to use the CTAPI.  
		/// However, while the connection is down, all functions will return failure.  
		/// If a connection cannot be created the first time ctOpen() is called, 
		/// a valid handle is still returned, however GetLastError() will indicate failure.
		/// </summary>
		/// <param name="host">
		/// For a local connection, specify NULL as the computer name. 
		/// The Windows Computer Name is the name as specified in the Identification tab, 
		/// under the Network section of the Windows Control Panel.
		/// For a remote connection, specify a TCP/IP address or DSN name.  
		/// The TCP/IP address (e.g. 10.5.6.7 or plant.yourdomain.com) can be determined 
		/// as follows:
		/// - For Windows NT4, go to the Command Prompt, type IPCONFIG, 
		/// and press the [Enter] key.
		/// - For Windows 95, select Start | Run, type WINIPCFG, and press the [Enter] key.
		/// - For Windows 98, select Start | Run, type WINIPCFG, and press the [Enter] key.
		/// </param>
		/// <param name="user">
		/// Your username as defined in the CitectHMI/SCADA project running on the computer 
		/// you wish to connect to.  This argument is only required if you are calling this 
		/// function from a remote computer.  On a local computer, it is optional.
		/// </param>
		/// <param name="password">
		/// Your password as defined in the CitectHMI/SCADA project running on the computer 
		/// you wish to connect to.  This argument is only required if you are calling this 
		/// function from a remote computer.  On a local computer, it is optional.
		/// </param>
		/// <param name="mode">The mode of the open.
		/// CT_OPEN_RECONNECT -  Reopen connection on failure. 
		/// If the connection to CitectHMI/SCADA is lost CTAPI will continue to 
		/// retry to connect to CitectHMI/SCADA.
		/// CT_OPEN_READ_ONLY - Open the CTAPI in read only mode.  
		/// This allows read only access to data - you cannot write to any variable in 
		/// CitectHMI/SCADA or call any Cicode function.
		/// CT_OPEN_BATCH - Disables the display of message boxes when an error occurs. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value specifies a handle.  
		/// If the function fails, the return value is NULL.  
		/// Use GetLastError() to get extended error information.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern uint ctOpen(  
			[MarshalAs(UnmanagedType.LPStr)]string host,  
			[MarshalAs(UnmanagedType.LPStr)]string user,  
			[MarshalAs(UnmanagedType.LPStr)]string password,  
			uint mode); 

		/// <summary>
		/// This function should be called to close the connection between the 
		/// application and the CTAPI.  When called, all pending commands will 
		/// be cancelled.  You must free any handles allocated before calling 
		/// ctClose().  These handles are not freed when ctClose() is called.  
		/// An application should call this function on shutdown or when a fatal 
		/// error occurs on the connection.
		/// </summary>
		/// <param name="hCTAPI">
		/// The handle to the CTAPI as returned from ctOpen().
		/// </param>
		/// <returns>
		/// TRUE if successful, otherwise FALSE.  Use GetLastError() to get 
		/// extended error information.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern bool ctClose(uint hCTAPI); 

		/// <summary>
		/// This function executes a Cicode function on the connected 
		/// CitectHMI/SCADA computer.  This allows you to control CitectHMI/SCADA 
		/// or to get information returned from Cicode functions. You may call 
		/// either built in or user defined Cicode functions. Cancels a pending 
		/// overlapped IO operation.
		/// The function name and arguments to that function are passed as a single 
		/// string.  Standard CitectHMI/SCADA conversion is applied to convert the 
		/// data from string type into the type expected by the function.  
		/// When passing strings you should put the strings between the CitectHMI/SCADA 
		/// string delimiters.
		/// Functions which expect pointers or arrays are not supported.  
		/// Functions which expect pointers are functions which update the arguments.  
		/// This includes functions DspGetMouse(), DspAnGetPos(), StrWord(), etc.  
		/// Functions which expect arrays to be passed or returned are not supported, 
		/// eg TableMath(), TrnSetTable(), TrnGetTable().  You may work around these 
		/// limitations by calling a Cicode wrapper function which inturn calls the 
		/// function you require.
		/// </summary>
		/// <param name="hCTAPI">The handle to the CTAPI as returned from ctOpen().</param>
		/// <param name="command">
		/// The Cicode function to execute.  
		/// The function name and arguments are passed as a string, 
		/// up to 4 kilobytes in length.  
		/// The string should only contain constant data - variables and functions 
		/// are not supported.  Pointers and arrays are also not supported.
		/// </param>
		/// <param name="hWin">
		/// The CitectHMI/SCADA window to execute the function.  
		/// This is a logical CitectHMI/SCADA window (0, 1, 2, 3 etc.) not a Windows Handle.
		/// </param>
		/// <param name="nMode">The mode of the Cicode call.  Set this to 0 (zero).</param>
		/// <param name="result">
		/// The buffer to put the result of the function call, 
		/// which is always returned as a string.  
		/// This may be NULL if you do not need the result of the function.
		/// </param>
		/// <param name="nMaxCount">
		/// The length of the sResult buffer.  
		/// If the result of the Cicode function is longer than the this number, 
		/// then the result is not returned and the function call fails, however 
		/// the Cicode function is still executed.  
		/// If the sResult is NULL then this length must be 0.
		/// </param>
		/// <param name="o">
		/// CTOVERLAPPED structure.  
		/// This structure is used to control the overlapped notification.  
		/// Set to NULL if you want a synchronous function call.
		/// </param>
		/// <returns>
		/// TRUE if successful, otherwise FALSE.  
		/// Use GetLastError() to get extended error information.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern uint ctCicode(  
			uint hCTAPI,
			[MarshalAs(UnmanagedType.LPStr)] string command,
			uint hWin, 
			int nMode, 
			[MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder result,  
			int nMaxCount,  
			[MarshalAs( UnmanagedType.AsAny )] Object o); 

		/// <summary>
		/// Writes to the given CitectHMI/SCADA I/O Device variable tag. 
		/// The value is converted into the correct data type, then scaled 
		/// and then written to the tag. If writing to an array element only 
		/// a single element of the array is written to.  This function will 
		/// generate a write request to the I/O Server.  The time taken to 
		/// complete this function will be dependent on the performance of 
		/// the I/O Device.  The calling thread is blocked until the write 
		/// is completed.
		/// </summary>
		/// <param name="hCTAPI">The handle to the CTAPI as returned from ctOpen().</param>
		/// <param name="tag">The tag name to write to.  You may use the array syntax [] to select an element of an array.</param>
		/// <param name="val">The value to write to the tag as a string.</param>
		/// <returns>
		/// TRUE if successful, otherwise FALSE.  Use GetLastError() to get 
		/// extended error information. 
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern bool ctTagWrite(  
			uint hCTAPI,
			[MarshalAs(UnmanagedType.LPStr)] string tag,
			[MarshalAs(UnmanagedType.LPStr)] string sValue);

		/// <summary>
		/// This function will read the single tag value.  
		/// The data will be returned in string format and scaled using the 
		/// CitectHMI/SCADA scales.
		/// The function will request the given tag from the CitectHMI/SCADA 
		/// I/O Server.  If the tag is in the I/O Servers device cache the data 
		/// will be returned from the cache.  If the tag is not in the device 
		/// cache then the tag will be read from the I/O Device.  The time taken 
		/// to complete this function will be dependent on the performance of 
		/// the I/O Device.  The calling thread is blocked until the read is 
		/// completed.
		/// </summary>
		/// <param name="hCTAPI">
		/// The handle to the CTAPI as returned from ctOpen().
		/// </param>
		/// <param name="tag">
		/// The CitectHMI/SCADA I/O Device tag to read.
		/// </param>
		/// <param name="result">
		/// The buffer to store the read data.  The data is returned in string format.
		/// </param>
		/// <param name="nMaxCount">
		/// The length of the read buffer.  If the read buffer is too small 
		/// the function will fail.
		/// </param>
		/// <returns>
		/// TRUE if successful, otherwise FALSE.  
		/// Use GetLastError() to get extended error information. 
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern bool ctTagRead(  
			uint hCTAPI,
			[MarshalAs(UnmanagedType.LPStr)] string tag,
			[MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder result,  
			int nMaxCount);

		/// <summary>
		/// Searches for the first object in the specified database, device, 
		/// trend, or alarm data which satisfies the filter string.  
		/// A handle to the found object is returned via pObjHnd.  
		/// The object handle is used to retrieve the object properties.  
		/// To find the next object, call the ctFindNext() function with 
		/// the returned search handle.  
		/// </summary>
		/// <remarks>
		/// If you experience server performance problems when using ctFindFirst()
		/// you should refer to CPULoadCount and CpuLoadSleepMS.
		/// </remarks>
		/// <param name="hCTAPI">The handle to the CTAPI as returned from ctOpen().</param>
		/// <param name="szTableName">
		/// The database, device, trend, or alarm data to be searched.  
		/// To search a database, simply enter the name of the database.  
		/// Currently the only table supported is TAG, which contains the list of 
		/// tags in the CitectHMI/SCADA project.
		/// 
		/// To search a device, simply enter the name as defined in the CitectHMI/SCADA 
		/// Devices form, e.g. "RECIPES" for the Example project.
		/// 
		///	To search trend data, you must specify the trend using the following format 
		///	(including the quotation marks):
		///	
		///	"CTAPITrend(sTime, sDate, Period, Length, Mode, Tag)"
		///	
		///	Where:
		///	sTime -	is the starting time for the trend.  
		///	Set the time to an empty string to search the most recent trend samples.
		///	sDate -	is the date of the trend.
		///	Period - is the period (in seconds) that you want to search (this period 
		///	can differ from the actual trend period).
		///	The Period argument used in the CTAPITrend() function must be 0 (zero) when 
		///	this function is used as an argument to ctFindFirst() for an EVENT trend query.
		///	Length - is the length of the data table, i.e. The number of rows of samples 
		///	to be searched.
		///	Mode - is the format mode to be used...
		///	
		///	Periodic trends
		///	1	Search the Date and Time, followed by the tags.
		///	2	Search the Time only, followed by the tags.
		///	4	Ignore any invalid or gated values.  
		///	(This mode is only supported for periodic trends.)
		///	
		///	Event trends
		///	1	Search the Time, Date, and Event Number, followed by the tags.
		///	2	Search the Time and Event Number, followed by the tags.
		///	
		///	Tag -	is the trend tag name for the data to be searched.
		///	
		///	To simplify the passing of this argument, you could first pass the CTAPITrend() 
		///	function as a string, then use the string as the szTableName argument 
		///	(without quotation marks). 
		///	
		///	To search alarm data, you must specify the alarm data using the following 
		///	format (including the quotation marks):
		///	
		///	"CTAPIAlarm(Category, Type, Area)"
		///	
		///	Where:
		///	Category -	is the alarm category or group number to match.  
		///	Set Category to 0 (zero) to match all alarm categories.
		///	Type -	is the type of alarms to find...
		///	Non-hardware alarms
		///	1	All unacknowledged alarms, ON and OFF.
		///	2	All acknowledged ON alarms.
		///	3	All disabled alarms.
		///	4	All configured alarms, i.e. Types 0 to 3, plus acknowledged OFF alarms.
		///	If you do not specify a Type, the default is 0.
		///	
		///	Area -	is the area in which to search for alarms.  
		///	If you do not specify an area, or if you set Area to -1, 
		///	only the current area will be searched.
		/// </param>
		/// <param name="szFilter">
		/// Filter criteria. This is a string based on the following format:
		/// "PropertyName1=FilterCriteria1;PropertyName2=FilterCriteria2"\.
		/// "*" as the filter to achieve the same result
		/// </param>
		/// <param name="pObjHnd">
		/// The pointer to the found object handle.  
		/// This is used to retrieve the properties.
		/// </param>
		/// <param name="dwFlags">
		/// This parameter is not yet supported.  
		/// Should be set to 0 (zero).
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is a search 
		/// handle used in a subsequent call to ctFindNext() or ctFindClose().  
		/// If the function fails, the return value is NULL.  
		/// To get extended error information, call GetLastError(). 
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern uint ctFindFirst(
			uint hCTAPI,
			[MarshalAs(UnmanagedType.LPStr)] string  szTableName,
			[MarshalAs(UnmanagedType.LPStr)] string  szFilter,
			ref uint pObjHnd,
			int dwFlags);

		/// <summary>
		/// Retrieves the next object in the search initiated by ctFindFirst()
		/// </summary>
		/// <param name="hnd">Handle to the search, as returned by ctFindFirst().</param>
		/// <param name="pObjHnd">
		/// The pointer to the found object handle.  
		/// This is used to retrieve the properties.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE (1). 
		/// If the function fails, the return value is FALSE (0). 
		/// To get extended error information, call GetLastError(). 
		/// If you reach the end of the search, GetLastError() returns CT_ERROR_NOT_FOUND. 
		/// Once past the end of the search, you cannot scroll the search using ctFindNext() 
		/// or ctFindPrev() commands. You must reset the search pointer by creating a new 
		/// search using ctFindFirst(), or by using the ctFindScroll() function to move the 
		/// pointer to a valid position.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern bool ctFindNext(uint hnd, ref uint pObjHnd);
		
		/// <summary>
		/// Retrieves the previous object in the search initiated by ctFindFirst().
		/// </summary>
		/// <param name="hnd">
		/// Handle to the search, as returned by ctFindFirst().
		/// </param>
		/// <param name="pObjHnd">
		/// The pointer to the found object handle.  This is used to retrieve the properties.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE (1). 
		/// If the function fails, the return value is FALSE (0). 
		/// To get extended error information, call GetLastError(). 
		/// If you reach the end of the search, GetLastError() returns CT_ERROR_NOT_FOUND. 
		/// Once past the end of the search, you cannot scroll the search using ctFindNext() 
		/// or ctFindPrev() commands. You must reset the search pointer by creating a new 
		/// search using ctFindFirst(), or by using the ctFindScroll() function to move the 
		/// pointer to a valid position.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern bool ctFindPrev(uint hnd, ref uint pObjHnd);

		/// <summary>
		/// Scrolls to the required object in the search initiated by ctFindFirst().
		/// To find the current scroll pointer, you can scroll relative 
		/// (dwMode = CT_FIND_SCROLL_RELATIVE) with an offset of 0.  
		/// To find the number of records returned in a search, scroll to the end of the search.
		/// </summary>
		/// <param name="hnd">Handle to the search, as returned by ctFindFirst().</param>
		/// <param name="nMode">
		/// Mode of the scroll.  The following modes are supported:
		/// 
		/// CT_FIND_SCROLL_NEXT: Scroll to the next record.  The dwOffset parameter is ignored.
		/// CT_FIND_SCROLL_PREV: Scroll to the previous record.  The dwOffset parameter is ignored.
		/// CT_FIND_SCROLL_FIRST: Scroll to the first record.  The dwOffset parameter is ignored.
		/// CT_FIND_SCROLL_LAST: Scroll to the last record.  The dwOffset parameter is ignored.
		/// CT_FIND_SCROLL_ABSOLUTE: Scroll to absolute record number.  
		/// The record number is specified in the dwOffset parameter.  
		/// The record number is from 1 to the maximum number of records returned in the search.
		/// CT_FIND_SCROLL_RELATIVE: Scroll relative records.  
		/// The number of records to scroll is specified by the dwOffset parameter.  
		/// If the offset is positive, this function will scroll to the next record, 
		/// if negative, it will scroll to the previous record.  
		/// If 0 (zero), no scrolling occurs.
		/// </param>
		/// <param name="dwOffset">
		/// Offset of the scroll.  
		/// The meaning of this parameter depends on the dwMode of the scrolling operation.
		/// </param>
		/// <param name="pObjHnd">
		/// The pointer to the found object handle.  This is used to retrieve the properties.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is non-zero.  
		/// If the function fails, the return value is zero.  
		/// To get extended error information, call GetLastError().  
		/// If no matching objects can be found, the GetLastError() function returns CT_ERROR_NOT_FOUND.  
		/// The return value is the current record number in the search.  
		/// Record numbers start at 1 (for the first record) and increment until the end 
		/// of the search has been reached.  
		/// Remember, 0 (zero) is not a valid record number - it signifies failure of the function.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError=true)] 
		public static extern int ctFindScroll(
			uint hnd,
			int nMode,
			long dwOffset,
			ref uint pObjHnd);

		/// <summary>
		/// Closes a search initiated by ctFindFirst().
		/// </summary>
		/// <param name="hnd">Handle to the search, as returned by ctFindFirst().</param>
		/// <returns>
		/// If the function succeeds, the return value is non-zero.  
		/// If the function fails, the return value is zero.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctFindClose(uint hnd);

		/// <summary>
		/// Retrieves an object property or meta data for an object.  
		/// This function should be used in conjunction with the ctFindFirst() 
		/// and ctFindNext() functions.  i.e. First, you find an object, 
		/// then you retrieve its properties.
		/// To retrieve property meta data such as type, size, etc., use the following 
		/// syntax for the szName argument:
		/// object.fields.count	// the number of fields in the record
		/// object.fields(n).name	// the name of the nth field of the record
		/// object.fields(n).type	// the type of the nth field of the record
		/// object.fields(n).actualsize	// the actual size of the nth field of the record
		/// </summary>
		/// <param name="hnd">Handle to the search, as returned by ctFindFirst().</param>
		/// <param name="sName">
		/// The name of the desired property, e.g. "TAG".  
		/// (An object might be a variable tag record, a trend tag record, 
		/// any RDB record, or a device object.)
		/// If you do not know the name of the property or you want to retrieve other 
		/// details about the property (meta data such as type, size, etc.), you can use 
		/// the following syntax:
		/// object.fields.count	// the number of fields in the record
		/// object.fields(n).name	// the name of the nth field of the record
		/// object.fields(n).type	// the type of the nth field of the record
		/// object.fields(n).actualsize	// the actual size of the nth field of the record
		/// </param>
		/// <param name="pData">
		/// The result buffer to store the read data.  
		/// The data is raw binary data, no data conversion or scaling is performed.  
		/// If this buffer is not large enough to receive the data, the data will be truncated, 
		/// and the function will return false.
		/// </param>
		/// <param name="dwBufferLength">
		/// Length of result buffer.  
		/// If the result buffer is not large enough to receive the data, 
		/// the data will be truncated, and the function will return false.
		/// </param>
		/// <param name="dwResultLength">
		/// Length of returned result.  You can pass NULL if you want to ignore this parameter.
		/// </param>
		/// <param name="dwType">
		/// The desired return type as follows:
		/// Value	Meaning
		/// DBTYPE_UI1	UCHAR.
		/// DBTYPE _I1	1 byte INT.
		/// DBTYPE _I2	2 byte INT.
		/// DBTYPE _I4	4 byte INT.
		/// DBTYPE _R4	4 byte REAL.
		/// DBTYPE _R8	8 byte REAL.
		/// DBTYPE _BOOL	BOOLEAN.
		/// DBTYPE_BYTES	Byte stream
		/// DBTYPE _STR	NULL Terminated STRING.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is non-zero.  
		/// If the function fails, the return value is zero.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctGetProperty(
			uint hnd,
			[MarshalAs(UnmanagedType.LPStr)] string  sName,
			[MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder pData,
			int dwBufferLength,
			ref int dwResultLength,
			[MarshalAs(UnmanagedType.I4)] CTAPI.DBTYPEENUM dwType);	

		/// <summary>
		/// Creates a new list.  The CTAPI provides three methods to read data from I/O Devices.  
		/// Each level varies in its complexity and performance.  The simplest way to read data 
		/// is via the ctTagRead() function.  This function reads the value of a single variable, 
		/// and the result is returned as a formatted engineering string.  
		/// The fastest way to read data is via the ctPointRead() function.  
		/// This function reads arrays of values, and the result is returned as a raw binary 
		/// format.  The Point functions are more complex to use, but perform better and support 
		/// overlapped operations.
		/// The List functions provide a middle ground between the Tag and the Point functions.  
		/// They provide a higher level of performance for reading data than the tag based 
		/// interface, and they are simpler to use than the point base interface.  
		/// The List functions also provide support for overlapped operations.  
		/// The List functions performance is very close to the level provided by the Point 
		/// functions.
		/// The list functions allow a group of tags to be defined and then read as a single 
		/// request.  They provide a simple tag based interface to data which is provided in 
		/// formatted engineering data.  You can create several lists and control each 
		/// individually. 
		/// </summary>
		/// <param name="hCTAPI">The handle to the CTAPI as returned from ctOpen().</param>
		/// <param name="nMode">
		/// The mode of the list creation.
		/// CT_LIST_EVENT - Create the list in event mode.  
		/// This mode allows you to use the ctListEvent() function.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value specifies a handle.  
		/// If the function fails, the return value is NULL.  
		/// To get extended error information, call GetLastError()
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern uint ctListNew(uint hCTAPI, int nMode);

		/// <summary>
		/// Frees a list created with ctListNew().  
		/// All tags added to the list are freed, you do not have to call ctListDelete() 
		/// for each tag.  You should not call ctListFree() while a read operation is pending.  
		/// Wait for the read to complete before freeing the list.
		/// </summary>
		/// <param name="hList">The handle to the list, as returned from ctListNew().</param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE.  
		/// If the function fails, the return value is FALSE.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctListFree(uint hList);

		/// <summary>
		/// Adds a tag to the list.  Once the tag has been added to the list, 
		/// it may be read using ctListRead() and written to using ctListWrite().  
		/// If a read is already pending, the tag will not be read until the next time ctListRead()
		///  is called.  ctListWrite() may be called immediately after the ctListAdd() function 
		///  has completed.
		/// </summary>
		/// <param name="hList">The handle to the list, as returned from ctListNew().</param>
		/// <param name="sTag">The tag to be added to the list.</param>
		/// <returns>
		/// If the function succeeds, the return value specifies a handle.  
		/// If the function fails, the return value is NULL.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern uint ctListAdd(
			uint hList,	
			[MarshalAs(UnmanagedType.LPStr)] string  sTag);

		/// <summary>
		/// Frees a tag created with ctListAdd().  
		/// It is safe to call ctListDelete() while a read or write is pending on another thread.  
		/// The ctListWrite() and ctListRead() will return once the tag has been deleted.
		/// </summary>
		/// <param name="hTag">The handle to the tag, as returned from ctListAdd().</param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE.  
		/// If the function fails, the return value is FALSE.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctListDelete(uint hTag);

		/// <summary>
		/// Reads all the tags on the list.  
		/// This function will read all tags which are attached to the list.  
		/// Once the data has been read from the I/O Devices, you may call ctListData()to get 
		/// the values of the tags.  If the read fails, ctListData() will return failure for 
		/// the tags that fail.
		/// While ctListRead() is pending you are allowed to add and delete tags from the list.  
		/// If you delete a tag from the list while ctListRead() is pending, it may still be 
		/// read one more time.  The next time ctListRead() is called, the tag will not be read.  
		/// If you add a tag to the list while ctListRead() is pending, the tag will not be read 
		/// until the next time ctListRead() is called.  You may call ctListData() for this tag 
		/// as soon as you have added it.  In this case ctListData() will fail, and GetLastError()
		/// will return GENERIC_INVALID_DATA.
		/// You can only have 1 pending read command on each list.  
		/// If you call ctListRead() again for the same list, the function will fail.
		/// Before freeing the list, you should make sure that there are no reads still pending.  
		/// You should wait for the any current ctListRead() to return and then delete the list.
		/// </summary>
		/// <param name="hList">The handle to the list, as returned from ctListNew().</param>
		/// <param name="o">
		/// CTOVERLAPPED structure.  
		/// This structure is used to control the overlapped notification.  
		/// Set to NULL if you want a synchronous function call. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE.  
		/// If the function fails, the return value is FALSE.  
		/// To get extended error information, call GetLastError().
		/// If an error occurred when reading any of the list data from the I/O Device, 
		/// the return value will be FALSE and GetLastError() will return the associated 
		/// CitectHMI/SCADA error code.  As a list can contain tags from many data sources, 
		/// some tags may be read correctly while other tags fail.  
		/// If any tag fails, ctListRead() will return FALSE, however, the other tags will 
		/// contain valid data.  You can call ctListData() to retrieve the value of each tag 
		/// and the individual error status for each tag on the list.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctListRead(
			uint hList, 
			[MarshalAs( UnmanagedType.AsAny )] Object o);

		/// <summary>
		/// Writes to a single tag on the list
		/// </summary>
		/// <param name="hTag">The handle to the tag, as returned from ctListAdd().</param>
		/// <param name="sValue">The handle to the tag, as returned from ctListAdd().</param>
		/// <param name="o">
		/// CTOVERLAPPED structure.  
		/// This structure is used to control the overlapped notification.  
		/// Set to NULL if you want a synchronous function call. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE.  
		/// If the function fails, the return value is FALSE.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctListWrite(
			uint hTag, 
			[MarshalAs(UnmanagedType.LPStr)] string sValue, 
			[MarshalAs( UnmanagedType.AsAny )] Object o);

		/// <summary>
		/// Gets the value of a tag on the list.  
		/// This function should be called after ctListRead() has completed for the added tag.  
		/// You may call ctListData() while subsequent ctListRead() functions are pending, 
		/// and the last data read will be returned.
		/// </summary>
		/// <param name="hTag">The handle to the tag, as returned from ctListAdd().</param>
		/// <param name="pBuffer">
		/// Pointer to a buffer to return the data.  
		/// The data is returned scaled and as a formatted string.
		/// </param>
		/// <param name="dwLength">The length of the buffer.</param>
		/// <param name="dwMode">
		/// Mode of the data.  
		/// The following modes are supported:
		/// 0 (zero):	The value is scaled using the scale specified in the CitectHMI/SCADA 
		/// project, and formatted using the format specified in the CitectHMI/SCADA project.
		/// FMT_NO_SCALE:	The value is not scaled even if the there is a scale specified in 
		/// CitectHMI/SCADA project.  The value will be formatted using the format specified 
		/// in the CitectHMI/SCADA project.
		/// FMT_NO_FORMAT:	The value is not formatted to the format specified in the 
		/// CitectHMI/SCADA project.  A default format is used.  If there is a scale specified 
		/// in the CitectHMI/SCADA project, it will be used to scale the value.
		/// You can use more than one mode by 'ORing'  them together.  
		/// e.g. FMT_NO_SCALE||FMT_NO_FORMAT.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is TRUE.  
		/// If the function fails, the return value is FALSE.  
		/// To get extended error information, call GetLastError().  
		/// If an error occurred when reading the data from the I/O Device, 
		/// the return value will be FALSE and GetLastError() will return the associated 
		/// CitectHMI/SCADA error code.
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctListData(
			uint hTag, [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder pBuffer, 
			int dwLength, 
			int dwMode);

		/// <summary>
		/// Returns the elements in the list which have changed state since they were last 
		/// read using the ctListRead() function. You must have created the list with 
		/// CT_LIST_EVENT mode in the ctListNew() function.
		/// </summary>
		/// <param name="hCTAPI">The handle to the CTAPI as returned from ctOpen().</param>
		/// <param name="dwMode">
		/// The mode of the list event.  
		/// You must use the same mode for all calls to ctListEvent() until NULL is returned 
		/// before changing mode.
		/// CT_LIST_EVENT_NEW - Get notifications when tags are added to the list.  
		/// When this mode is used, you will get an event message when new tags added to the list.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value specifies a handle to a tag which 
		/// has changed state since the last time ctListRead was called.  
		/// If the function fails or there are no changes, the return value is NULL.  
		/// To get extended error information, call GetLastError().
		/// </returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern uint ctListEvent(uint hCTAPI, int dwMode);

		/// <summary>
		/// Returns number of licenses.
		/// </summary>
		/// <param name="hCTAPI">The handle to the CTAPI as returned from ctOpen().</param>
		/// <param name="nLicenses">undocumented</param>
		/// <param name="b">undocumented</param>
		/// <returns>undocumented</returns>
		[DllImport("ctapi.dll", SetLastError = true)] 
		public static extern bool ctGetNumberOfLicenses(
			uint hCTAPI, 
			ref short nLicenses, 
			byte b);

		#endregion // Public Methods

		#region Protected and Internal Methods ---------------------------------------
		#endregion // Protected and Internal Methods

		#region Private Methods ------------------------------------------------------
		#endregion // Private Methods
	}
}

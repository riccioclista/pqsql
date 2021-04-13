using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace Pqsql
{
	// enum PostgresPollingStatusType from libpq-fe.h
	internal enum PostgresPollingStatus
	{
		PGRES_POLLING_FAILED = 0,
		PGRES_POLLING_READING,      /* These two indicate that one may    */
		PGRES_POLLING_WRITING,      /* use select before polling again.   */
		PGRES_POLLING_OK,
		PGRES_POLLING_ACTIVE        /* unused; keep for awhile for backwards compatibility */
	};

	// enum ConnectionStatusType from libpq-fe.h
	internal enum ConnStatusType
	{
		CONNECTION_OK = 0,
		CONNECTION_BAD,
		/* Non-blocking mode only below here */

		/*
		 * The existence of these should never be relied upon - they should only
		 * be used for user feedback or similar purposes.
		 */
		CONNECTION_STARTED,			/* Waiting for connection to be made.  */
		CONNECTION_MADE,			  /* Connection OK; waiting to send.     */
		CONNECTION_AWAITING_RESPONSE,		/* Waiting for a response from the postmaster. */
		CONNECTION_AUTH_OK,			/* Received authentication; waiting for backend startup. */
		CONNECTION_SETENV,			/* Negotiating environment. */
		CONNECTION_SSL_STARTUP,	/* Negotiating SSL. */
		CONNECTION_NEEDED			  /* Internal state: connect() needed */
	};

	// enum ExecStatusType from libpq-fe.h
	internal enum ExecStatusType
	{
		PGRES_EMPTY_QUERY = 0, /* empty query string was executed */
		PGRES_COMMAND_OK,			 /* a query command that doesn't return anything was executed properly by the backend */
		PGRES_TUPLES_OK,			 /* a query command that returns tuples was executed properly by the backend, PGresult contains the result tuples */
		PGRES_COPY_OUT,				 /* Copy Out data transfer in progress */
		PGRES_COPY_IN,				 /* Copy In data transfer in progress */
		PGRES_BAD_RESPONSE,		 /* an unexpected response was recv'd from the backend */
		PGRES_NONFATAL_ERROR,	 /* notice or warning message */
		PGRES_FATAL_ERROR,		 /* query failed */
		PGRES_COPY_BOTH,			 /* Copy In/Out data transfer in progress */
		PGRES_SINGLE_TUPLE		 /* single tuple from larger resultset */
	};

	// see enum PGTransactionStatusType in libpq-fe.h
	internal enum PGTransactionStatusType
	{
		PQTRANS_IDLE,               /* connection idle */
		PQTRANS_ACTIVE,             /* command in progress */
		PQTRANS_INTRANS,            /* idle, within transaction block */
		PQTRANS_INERROR,            /* idle, within failed transaction */
		PQTRANS_UNKNOWN             /* cannot determine status */
	};

	// see enum pg_enc in pg_wchar.h
	internal enum PgEnc
	{
		PG_UTF8 = 6
	}

	// see struct pg_encname and pg_encname_tbl in pg_wchar.h
	internal static class PgEncName
	{
		public static readonly byte[] PG_UTF8 = PqsqlUTF8Statement.CreateUTF8Statement("utf8");
	}

	/// <summary>
	/// preprocessor macros from postgres_ext.h used in PQresultErrorField
	/// 
	/// generated from src/include/postgres_ext.h using
	/// 
	/// awk '$2 ~ /PG_DIAG_/{printf "\t\tpublic const char %s = %s;\n",$2,$3;}' src/include/postgres_ext.h
	/// </summary>
	internal class PqsqlDiag
	{
		// private default ctor
		private PqsqlDiag()
		{
		}

		public const char PG_DIAG_SEVERITY = 'S';
		public const char PG_DIAG_SEVERITY_NONLOCALIZED = 'V';
		public const char PG_DIAG_SQLSTATE = 'C';
		public const char PG_DIAG_MESSAGE_PRIMARY = 'M';
		public const char PG_DIAG_MESSAGE_DETAIL = 'D';
		public const char PG_DIAG_MESSAGE_HINT = 'H';
		public const char PG_DIAG_STATEMENT_POSITION = 'P';
		public const char PG_DIAG_INTERNAL_POSITION = 'p';
		public const char PG_DIAG_INTERNAL_QUERY = 'q';
		public const char PG_DIAG_CONTEXT = 'W';
		public const char PG_DIAG_SCHEMA_NAME = 's';
		public const char PG_DIAG_TABLE_NAME = 't';
		public const char PG_DIAG_COLUMN_NAME = 'c';
		public const char PG_DIAG_DATATYPE_NAME = 'd';
		public const char PG_DIAG_CONSTRAINT_NAME = 'n';
		public const char PG_DIAG_SOURCE_FILE = 'F';
		public const char PG_DIAG_SOURCE_LINE = 'L';
		public const char PG_DIAG_SOURCE_FUNCTION = 'R';
	}

	/// <summary>
	/// PG_DIAG_SQLSTATE strings encoded as integers (PqsqlException.ErrorCode)
	///
	/// generated from src/backend/utils/errcodes.txt using
	/// 
	/// egrep -v '^#' src/backend/utils/errcodes.txt | awk 'BEGIN { convert="0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" }
	///      function code2int(c)
	///      {
	///        split(c,ch,"");
	///        n=0;
	///        for(i=0; i != 5; i++) {
	///           j = index(convert, ch[i+1]) - 1;
	///           n = or(n, lshift(j, i*6));
	///        }
	///        return n;
	///      }
	///      /^Section: / {
	///        if (region) printf "\t\t#endregion\n\n";
	///        print "\t\t#region", substr($0,10);
	///        region = 1;
	///      }
	///      $1 ~ /^[A-Z0-9][A-Z0-9][A-Z0-9][A-Z0-9][A-Z0-9]$/ {
	///        v = substr($3,9);
	///        printf "\t\t%s = %d, // %s (%s)\n",v,code2int($1),$1,$4;
	///      }
	///      END {
	///        print "\t\t#endregion";
	///      }'
	/// </summary>
	/// <remarks>
	/// https://www.postgresql.org/docs/current/static/errcodes-appendix.html
	/// </remarks>
	/// <seealso cref="PqsqlException.CreateErrorCode"/>
	public enum PqsqlState
	{
		#region Soon to be obsolete

		// use PqsqlState.T_R_DEADLOCK_DETECTED instead
		DEADLOCK_DETECTED = 16879620, // 40P01 (deadlock_detected)

		#endregion

		#region Class 00 - Successful Completion
		SUCCESSFUL_COMPLETION = 0, // 00000 (successful_completion)
		#endregion

		#region Class 01 - Warning
		WARNING = 64, // 01000 (warning)
		WARNING_DYNAMIC_RESULT_SETS_RETURNED = 201326656, // 0100C (dynamic_result_sets_returned)
		WARNING_IMPLICIT_ZERO_BIT_PADDING = 134217792, // 01008 (implicit_zero_bit_padding)
		WARNING_NULL_VALUE_ELIMINATED_IN_SET_FUNCTION = 50331712, // 01003 (null_value_eliminated_in_set_function)
		WARNING_PRIVILEGE_NOT_GRANTED = 117440576, // 01007 (privilege_not_granted)
		WARNING_PRIVILEGE_NOT_REVOKED = 100663360, // 01006 (privilege_not_revoked)
		WARNING_STRING_DATA_RIGHT_TRUNCATION = 67108928, // 01004 (string_data_right_truncation)
		WARNING_DEPRECATED_FEATURE = 16879680, // 01P01 (deprecated_feature)
		#endregion

		#region Class 02 - No Data (this is also a warning class per the SQL standard)
		NO_DATA = 128, // 02000 (no_data)
		NO_ADDITIONAL_DYNAMIC_RESULT_SETS_RETURNED = 16777344, // 02001 (no_additional_dynamic_result_sets_returned)
		#endregion

		#region Class 03 - SQL Statement Not Yet Complete
		SQL_STATEMENT_NOT_YET_COMPLETE = 192, // 03000 (sql_statement_not_yet_complete)
		#endregion

		#region Class 08 - Connection Exception
		CONNECTION_EXCEPTION = 512, // 08000 (connection_exception)
		CONNECTION_DOES_NOT_EXIST = 50332160, // 08003 (connection_does_not_exist)
		CONNECTION_FAILURE = 100663808, // 08006 (connection_failure)
		SQLCLIENT_UNABLE_TO_ESTABLISH_SQLCONNECTION = 16777728, // 08001 (sqlclient_unable_to_establish_sqlconnection)
		SQLSERVER_REJECTED_ESTABLISHMENT_OF_SQLCONNECTION = 67109376, // 08004 (sqlserver_rejected_establishment_of_sqlconnection)
		TRANSACTION_RESOLUTION_UNKNOWN = 117441024, // 08007 (transaction_resolution_unknown)
		PROTOCOL_VIOLATION = 16880128, // 08P01 (protocol_violation)
		#endregion

		#region Class 09 - Triggered Action Exception
		TRIGGERED_ACTION_EXCEPTION = 576, // 09000 (triggered_action_exception)
		#endregion

		#region Class 0A - Feature Not Supported
		FEATURE_NOT_SUPPORTED = 640, // 0A000 (feature_not_supported)
		#endregion

		#region Class 0B - Invalid Transaction Initiation
		INVALID_TRANSACTION_INITIATION = 704, // 0B000 (invalid_transaction_initiation)
		#endregion

		#region Class 0F - Locator Exception
		LOCATOR_EXCEPTION = 960, // 0F000 (locator_exception)
		L_E_INVALID_SPECIFICATION = 16778176, // 0F001 (invalid_locator_specification)
		#endregion

		#region Class 0L - Invalid Grantor
		INVALID_GRANTOR = 1344, // 0L000 (invalid_grantor)
		INVALID_GRANT_OPERATION = 16880960, // 0LP01 (invalid_grant_operation)
		#endregion

		#region Class 0P - Invalid Role Specification
		INVALID_ROLE_SPECIFICATION = 1600, // 0P000 (invalid_role_specification)
		#endregion

		#region Class 0Z - Diagnostics Exception
		DIAGNOSTICS_EXCEPTION = 2240, // 0Z000 (diagnostics_exception)
		STACKED_DIAGNOSTICS_ACCESSED_WITHOUT_ACTIVE_HANDLER = 33556672, // 0Z002 (stacked_diagnostics_accessed_without_active_handler)
		#endregion

		#region Class 20 - Case Not Found
		CASE_NOT_FOUND = 2, // 20000 (case_not_found)
		#endregion

		#region Class 21 - Cardinality Violation
		CARDINALITY_VIOLATION = 66, // 21000 (cardinality_violation)
		#endregion

		#region Class 22 - Data Exception
		DATA_EXCEPTION = 130, // 22000 (data_exception)
		ARRAY_ELEMENT_ERROR = 235405442, // 2202E ()
		ARRAY_SUBSCRIPT_ERROR = 235405442, // 2202E (array_subscript_error)
		CHARACTER_NOT_IN_REPERTOIRE = 17301634, // 22021 (character_not_in_repertoire)
		DATETIME_FIELD_OVERFLOW = 134217858, // 22008 (datetime_field_overflow)
		DATETIME_VALUE_OUT_OF_RANGE = 134217858, // 22008 ()
		DIVISION_BY_ZERO = 33816706, // 22012 (division_by_zero)
		ERROR_IN_ASSIGNMENT = 83886210, // 22005 (error_in_assignment)
		ESCAPE_CHARACTER_CONFLICT = 184549506, // 2200B (escape_character_conflict)
		INDICATOR_OVERFLOW = 34078850, // 22022 (indicator_overflow)
		INTERVAL_FIELD_OVERFLOW = 84148354, // 22015 (interval_field_overflow)
		INVALID_ARGUMENT_FOR_LOG = 235143298, // 2201E (invalid_argument_for_logarithm)
		INVALID_ARGUMENT_FOR_NTILE = 67371138, // 22014 (invalid_argument_for_ntile_function)
		INVALID_ARGUMENT_FOR_NTH_VALUE = 100925570, // 22016 (invalid_argument_for_nth_value_function)
		INVALID_ARGUMENT_FOR_POWER_FUNCTION = 251920514, // 2201F (invalid_argument_for_power_function)
		INVALID_ARGUMENT_FOR_WIDTH_BUCKET_FUNCTION = 268697730, // 2201G (invalid_argument_for_width_bucket_function)
		INVALID_CHARACTER_VALUE_FOR_CAST = 134480002, // 22018 (invalid_character_value_for_cast)
		INVALID_DATETIME_FORMAT = 117440642, // 22007 (invalid_datetime_format)
		INVALID_ESCAPE_CHARACTER = 151257218, // 22019 (invalid_escape_character)
		INVALID_ESCAPE_OCTET = 218103938, // 2200D (invalid_escape_octet)
		INVALID_ESCAPE_SEQUENCE = 84410498, // 22025 (invalid_escape_sequence)
		NONSTANDARD_USE_OF_ESCAPE_CHARACTER = 100765826, // 22P06 (nonstandard_use_of_escape_character)
		INVALID_INDICATOR_PARAMETER_VALUE = 262274, // 22010 (invalid_indicator_parameter_value)
		INVALID_PARAMETER_VALUE = 50856066, // 22023 (invalid_parameter_value)
		INVALID_REGULAR_EXPRESSION = 184811650, // 2201B (invalid_regular_expression)
		INVALID_ROW_COUNT_IN_LIMIT_CLAUSE = 537133186, // 2201W (invalid_row_count_in_limit_clause)
		INVALID_ROW_COUNT_IN_RESULT_OFFSET_CLAUSE = 553910402, // 2201X (invalid_row_count_in_result_offset_clause)
		INVALID_TABLESAMPLE_ARGUMENT = 285737090, // 2202H (invalid_tablesample_argument)
		INVALID_TABLESAMPLE_REPEAT = 268959874, // 2202G (invalid_tablesample_repeat)
		INVALID_TIME_ZONE_DISPLACEMENT_VALUE = 150995074, // 22009 (invalid_time_zone_displacement_value)
		INVALID_USE_OF_ESCAPE_CHARACTER = 201326722, // 2200C (invalid_use_of_escape_character)
		MOST_SPECIFIC_TYPE_MISMATCH = 268435586, // 2200G (most_specific_type_mismatch)
		NULL_VALUE_NOT_ALLOWED = 67108994, // 22004 (null_value_not_allowed)
		NULL_VALUE_NO_INDICATOR_PARAMETER = 33554562, // 22002 (null_value_no_indicator_parameter)
		NUMERIC_VALUE_OUT_OF_RANGE = 50331778, // 22003 (numeric_value_out_of_range)
		STRING_DATA_LENGTH_MISMATCH = 101187714, // 22026 (string_data_length_mismatch)
		STRING_DATA_RIGHT_TRUNCATION = 16777346, // 22001 (string_data_right_truncation)
		SUBSTRING_ERROR = 17039490, // 22011 (substring_error)
		TRIM_ERROR = 117964930, // 22027 (trim_error)
		UNTERMINATED_C_STRING = 67633282, // 22024 (unterminated_c_string)
		ZERO_LENGTH_CHARACTER_STRING = 251658370, // 2200F (zero_length_character_string)
		FLOATING_POINT_EXCEPTION = 16879746, // 22P01 (floating_point_exception)
		INVALID_TEXT_REPRESENTATION = 33656962, // 22P02 (invalid_text_representation)
		INVALID_BINARY_REPRESENTATION = 50434178, // 22P03 (invalid_binary_representation)
		BAD_COPY_FILE_FORMAT = 67211394, // 22P04 (bad_copy_file_format)
		UNTRANSLATABLE_CHARACTER = 83988610, // 22P05 (untranslatable_character)
		NOT_AN_XML_DOCUMENT = 352321666, // 2200L (not_an_xml_document)
		INVALID_XML_DOCUMENT = 369098882, // 2200M (invalid_xml_document)
		INVALID_XML_CONTENT = 385876098, // 2200N (invalid_xml_content)
		INVALID_XML_COMMENT = 469762178, // 2200S (invalid_xml_comment)
		INVALID_XML_PROCESSING_INSTRUCTION = 486539394, // 2200T (invalid_xml_processing_instruction)
		#endregion

		#region Class 23 - Integrity Constraint Violation
		INTEGRITY_CONSTRAINT_VIOLATION = 194, // 23000 (integrity_constraint_violation)
		RESTRICT_VIOLATION = 16777410, // 23001 (restrict_violation)
		NOT_NULL_VIOLATION = 33575106, // 23502 (not_null_violation)
		FOREIGN_KEY_VIOLATION = 50352322, // 23503 (foreign_key_violation)
		UNIQUE_VIOLATION = 83906754, // 23505 (unique_violation)
		CHECK_VIOLATION = 67391682, // 23514 (check_violation)
		EXCLUSION_VIOLATION = 16879810, // 23P01 (exclusion_violation)
		#endregion

		#region Class 24 - Invalid Cursor State
		INVALID_CURSOR_STATE = 258, // 24000 (invalid_cursor_state)
		#endregion

		#region Class 25 - Invalid Transaction State
		INVALID_TRANSACTION_STATE = 322, // 25000 (invalid_transaction_state)
		ACTIVE_SQL_TRANSACTION = 16777538, // 25001 (active_sql_transaction)
		BRANCH_TRANSACTION_ALREADY_ACTIVE = 33554754, // 25002 (branch_transaction_already_active)
		HELD_CURSOR_REQUIRES_SAME_ISOLATION_LEVEL = 134218050, // 25008 (held_cursor_requires_same_isolation_level)
		INAPPROPRIATE_ACCESS_MODE_FOR_BRANCH_TRANSACTION = 50331970, // 25003 (inappropriate_access_mode_for_branch_transaction)
		INAPPROPRIATE_ISOLATION_LEVEL_FOR_BRANCH_TRANSACTION = 67109186, // 25004 (inappropriate_isolation_level_for_branch_transaction)
		NO_ACTIVE_SQL_TRANSACTION_FOR_BRANCH_TRANSACTION = 83886402, // 25005 (no_active_sql_transaction_for_branch_transaction)
		READ_ONLY_SQL_TRANSACTION = 100663618, // 25006 (read_only_sql_transaction)
		SCHEMA_AND_DATA_STATEMENT_MIXING_NOT_SUPPORTED = 117440834, // 25007 (schema_and_data_statement_mixing_not_supported)
		NO_ACTIVE_SQL_TRANSACTION = 16879938, // 25P01 (no_active_sql_transaction)
		IN_FAILED_SQL_TRANSACTION = 33657154, // 25P02 (in_failed_sql_transaction)
		IDLE_IN_TRANSACTION_SESSION_TIMEOUT = 50434370, // 25P03 (idle_in_transaction_session_timeout)
		#endregion

		#region Class 26 - Invalid SQL Statement Name
		INVALID_SQL_STATEMENT_NAME = 386, // 26000 (invalid_sql_statement_name)
		#endregion

		#region Class 27 - Triggered Data Change Violation
		TRIGGERED_DATA_CHANGE_VIOLATION = 450, // 27000 (triggered_data_change_violation)
		#endregion

		#region Class 28 - Invalid Authorization Specification
		INVALID_AUTHORIZATION_SPECIFICATION = 514, // 28000 (invalid_authorization_specification)
		INVALID_PASSWORD = 16880130, // 28P01 (invalid_password)
		#endregion

		#region Class 2B - Dependent Privilege Descriptors Still Exist
		DEPENDENT_PRIVILEGE_DESCRIPTORS_STILL_EXIST = 706, // 2B000 (dependent_privilege_descriptors_still_exist)
		DEPENDENT_OBJECTS_STILL_EXIST = 16880322, // 2BP01 (dependent_objects_still_exist)
		#endregion

		#region Class 2D - Invalid Transaction Termination
		INVALID_TRANSACTION_TERMINATION = 834, // 2D000 (invalid_transaction_termination)
		#endregion

		#region Class 2F - SQL Routine Exception
		SQL_ROUTINE_EXCEPTION = 962, // 2F000 (sql_routine_exception)
		S_R_E_FUNCTION_EXECUTED_NO_RETURN_STATEMENT = 83887042, // 2F005 (function_executed_no_return_statement)
		S_R_E_MODIFYING_SQL_DATA_NOT_PERMITTED = 33555394, // 2F002 (modifying_sql_data_not_permitted)
		S_R_E_PROHIBITED_SQL_STATEMENT_ATTEMPTED = 50332610, // 2F003 (prohibited_sql_statement_attempted)
		S_R_E_READING_SQL_DATA_NOT_PERMITTED = 67109826, // 2F004 (reading_sql_data_not_permitted)
		#endregion

		#region Class 34 - Invalid Cursor Name
		INVALID_CURSOR_NAME = 259, // 34000 (invalid_cursor_name)
		#endregion

		#region Class 38 - External Routine Exception
		EXTERNAL_ROUTINE_EXCEPTION = 515, // 38000 (external_routine_exception)
		E_R_E_CONTAINING_SQL_NOT_PERMITTED = 16777731, // 38001 (containing_sql_not_permitted)
		E_R_E_MODIFYING_SQL_DATA_NOT_PERMITTED = 33554947, // 38002 (modifying_sql_data_not_permitted)
		E_R_E_PROHIBITED_SQL_STATEMENT_ATTEMPTED = 50332163, // 38003 (prohibited_sql_statement_attempted)
		E_R_E_READING_SQL_DATA_NOT_PERMITTED = 67109379, // 38004 (reading_sql_data_not_permitted)
		#endregion

		#region Class 39 - External Routine Invocation Exception
		EXTERNAL_ROUTINE_INVOCATION_EXCEPTION = 579, // 39000 (external_routine_invocation_exception)
		E_R_I_E_INVALID_SQLSTATE_RETURNED = 16777795, // 39001 (invalid_sqlstate_returned)
		E_R_I_E_NULL_VALUE_NOT_ALLOWED = 67109443, // 39004 (null_value_not_allowed)
		E_R_I_E_TRIGGER_PROTOCOL_VIOLATED = 16880195, // 39P01 (trigger_protocol_violated)
		E_R_I_E_SRF_PROTOCOL_VIOLATED = 33657411, // 39P02 (srf_protocol_violated)
		E_R_I_E_EVENT_TRIGGER_PROTOCOL_VIOLATED = 50434627, // 39P03 (event_trigger_protocol_violated)
		#endregion

		#region Class 3B - Savepoint Exception
		SAVEPOINT_EXCEPTION = 707, // 3B000 (savepoint_exception)
		S_E_INVALID_SPECIFICATION = 16777923, // 3B001 (invalid_savepoint_specification)
		#endregion

		#region Class 3D - Invalid Catalog Name
		INVALID_CATALOG_NAME = 835, // 3D000 (invalid_catalog_name)
		#endregion

		#region Class 3F - Invalid Schema Name
		INVALID_SCHEMA_NAME = 963, // 3F000 (invalid_schema_name)
		#endregion

		#region Class 40 - Transaction Rollback
		TRANSACTION_ROLLBACK = 4, // 40000 (transaction_rollback)
		T_R_INTEGRITY_CONSTRAINT_VIOLATION = 33554436, // 40002 (transaction_integrity_constraint_violation)
		T_R_SERIALIZATION_FAILURE = 16777220, // 40001 (serialization_failure)
		T_R_STATEMENT_COMPLETION_UNKNOWN = 50331652, // 40003 (statement_completion_unknown)
		T_R_DEADLOCK_DETECTED = 16879620, // 40P01 (deadlock_detected)
		#endregion

		#region Class 42 - Syntax Error or Access Rule Violation
		SYNTAX_ERROR_OR_ACCESS_RULE_VIOLATION = 132, // 42000 (syntax_error_or_access_rule_violation)
		SYNTAX_ERROR = 16801924, // 42601 (syntax_error)
		INSUFFICIENT_PRIVILEGE = 16797828, // 42501 (insufficient_privilege)
		CANNOT_COERCE = 101744772, // 42846 (cannot_coerce)
		GROUPING_ERROR = 50364548, // 42803 (grouping_error)
		WINDOWING_ERROR = 626820, // 42P20 (windowing_error)
		INVALID_RECURSION = 151359620, // 42P19 (invalid_recursion)
		INVALID_FOREIGN_KEY = 819332, // 42830 (invalid_foreign_key)
		INVALID_NAME = 33579140, // 42602 (invalid_name)
		NAME_TOO_LONG = 34103428, // 42622 (name_too_long)
		RESERVED_NAME = 151818372, // 42939 (reserved_name)
		DATATYPE_MISMATCH = 67141764, // 42804 (datatype_mismatch)
		INDETERMINATE_DATATYPE = 134582404, // 42P18 (indeterminate_datatype)
		COLLATION_MISMATCH = 17404036, // 42P21 (collation_mismatch)
		INDETERMINATE_COLLATION = 34181252, // 42P22 (indeterminate_collation)
		WRONG_OBJECT_TYPE = 151027844, // 42809 (wrong_object_type)
		UNDEFINED_COLUMN = 50360452, // 42703 (undefined_column)
		UNDEFINED_CURSOR = 259, // 34000 ()
		UNDEFINED_DATABASE = 835, // 3D000 ()
		UNDEFINED_FUNCTION = 52461700, // 42883 (undefined_function)
		UNDEFINED_PSTATEMENT = 386, // 26000 ()
		UNDEFINED_SCHEMA = 963, // 3F000 ()
		UNDEFINED_TABLE = 16879748, // 42P01 (undefined_table)
		UNDEFINED_PARAMETER = 33656964, // 42P02 (undefined_parameter)
		UNDEFINED_OBJECT = 67137668, // 42704 (undefined_object)
		DUPLICATE_COLUMN = 16806020, // 42701 (duplicate_column)
		DUPLICATE_CURSOR = 50434180, // 42P03 (duplicate_cursor)
		DUPLICATE_DATABASE = 67211396, // 42P04 (duplicate_database)
		DUPLICATE_FUNCTION = 50884740, // 42723 (duplicate_function)
		DUPLICATE_PSTATEMENT = 83988612, // 42P05 (duplicate_prepared_statement)
		DUPLICATE_SCHEMA = 100765828, // 42P06 (duplicate_schema)
		DUPLICATE_TABLE = 117543044, // 42P07 (duplicate_table)
		DUPLICATE_ALIAS = 33845380, // 42712 (duplicate_alias)
		DUPLICATE_OBJECT = 290948, // 42710 (duplicate_object)
		AMBIGUOUS_COLUMN = 33583236, // 42702 (ambiguous_column)
		AMBIGUOUS_FUNCTION = 84439172, // 42725 (ambiguous_function)
		AMBIGUOUS_PARAMETER = 134320260, // 42P08 (ambiguous_parameter)
		AMBIGUOUS_ALIAS = 151097476, // 42P09 (ambiguous_alias)
		INVALID_COLUMN_REFERENCE = 364676, // 42P10 (invalid_column_reference)
		INVALID_COLUMN_DEFINITION = 17064068, // 42611 (invalid_column_definition)
		INVALID_CURSOR_DEFINITION = 17141892, // 42P11 (invalid_cursor_definition)
		INVALID_DATABASE_DEFINITION = 33919108, // 42P12 (invalid_database_definition)
		INVALID_FUNCTION_DEFINITION = 50696324, // 42P13 (invalid_function_definition)
		INVALID_PSTATEMENT_DEFINITION = 67473540, // 42P14 (invalid_prepared_statement_definition)
		INVALID_SCHEMA_DEFINITION = 84250756, // 42P15 (invalid_schema_definition)
		INVALID_TABLE_DEFINITION = 101027972, // 42P16 (invalid_table_definition)
		INVALID_OBJECT_DEFINITION = 117805188, // 42P17 (invalid_object_definition)
		#endregion

		#region Class 44 - WITH CHECK OPTION Violation
		WITH_CHECK_OPTION_VIOLATION = 260, // 44000 (with_check_option_violation)
		#endregion

		#region Class 53 - Insufficient Resources
		INSUFFICIENT_RESOURCES = 197, // 53000 (insufficient_resources)
		DISK_FULL = 4293, // 53100 (disk_full)
		OUT_OF_MEMORY = 8389, // 53200 (out_of_memory)
		TOO_MANY_CONNECTIONS = 12485, // 53300 (too_many_connections)
		CONFIGURATION_LIMIT_EXCEEDED = 16581, // 53400 (configuration_limit_exceeded)
		#endregion

		#region Class 54 - Program Limit Exceeded
		PROGRAM_LIMIT_EXCEEDED = 261, // 54000 (program_limit_exceeded)
		STATEMENT_TOO_COMPLEX = 16777477, // 54001 (statement_too_complex)
		TOO_MANY_COLUMNS = 17039621, // 54011 (too_many_columns)
		TOO_MANY_ARGUMENTS = 50856197, // 54023 (too_many_arguments)
		#endregion

		#region Class 55 - Object Not In Prerequisite State
		OBJECT_NOT_IN_PREREQUISITE_STATE = 325, // 55000 (object_not_in_prerequisite_state)
		OBJECT_IN_USE = 100663621, // 55006 (object_in_use)
		CANT_CHANGE_RUNTIME_PARAM = 33657157, // 55P02 (cant_change_runtime_param)
		LOCK_NOT_AVAILABLE = 50434373, // 55P03 (lock_not_available)
		#endregion

		#region Class 57 - Operator Intervention
		OPERATOR_INTERVENTION = 453, // 57000 (operator_intervention)
		QUERY_CANCELED = 67371461, // 57014 (query_canceled)
		ADMIN_SHUTDOWN = 16880069, // 57P01 (admin_shutdown)
		CRASH_SHUTDOWN = 33657285, // 57P02 (crash_shutdown)
		CANNOT_CONNECT_NOW = 50434501, // 57P03 (cannot_connect_now)
		DATABASE_DROPPED = 67211717, // 57P04 (database_dropped)
		#endregion

		#region Class 58 - System Error (errors external to PostgreSQL itself)
		SYSTEM_ERROR = 517, // 58000 (system_error)
		IO_ERROR = 786949, // 58030 (io_error)
		UNDEFINED_FILE = 16880133, // 58P01 (undefined_file)
		DUPLICATE_FILE = 33657349, // 58P02 (duplicate_file)
		#endregion

		#region Class 72 - Snapshot Failure
		SNAPSHOT_TOO_OLD = 135, // 72000 (snapshot_too_old)
		#endregion

		#region Class F0 - Configuration File Error
		CONFIG_FILE_ERROR = 15, // F0000 (config_file_error)
		LOCK_FILE_EXISTS = 16777231, // F0001 (lock_file_exists)
		#endregion

		#region Class HV - Foreign Data Wrapper Error (SQL/MED)
		FDW_ERROR = 2001, // HV000 (fdw_error)
		FDW_COLUMN_NAME_NOT_FOUND = 83888081, // HV005 (fdw_column_name_not_found)
		FDW_DYNAMIC_PARAMETER_VALUE_NEEDED = 33556433, // HV002 (fdw_dynamic_parameter_value_needed)
		FDW_FUNCTION_SEQUENCE_ERROR = 264145, // HV010 (fdw_function_sequence_error)
		FDW_INCONSISTENT_DESCRIPTOR_INFORMATION = 17303505, // HV021 (fdw_inconsistent_descriptor_information)
		FDW_INVALID_ATTRIBUTE_VALUE = 67635153, // HV024 (fdw_invalid_attribute_value)
		FDW_INVALID_COLUMN_NAME = 117442513, // HV007 (fdw_invalid_column_name)
		FDW_INVALID_COLUMN_NUMBER = 134219729, // HV008 (fdw_invalid_column_number)
		FDW_INVALID_DATA_TYPE = 67110865, // HV004 (fdw_invalid_data_type)
		FDW_INVALID_DATA_TYPE_DESCRIPTORS = 100665297, // HV006 (fdw_invalid_data_type_descriptors)
		FDW_INVALID_DESCRIPTOR_FIELD_IDENTIFIER = 19138513, // HV091 (fdw_invalid_descriptor_field_identifier)
		FDW_INVALID_HANDLE = 184551377, // HV00B (fdw_invalid_handle)
		FDW_INVALID_OPTION_INDEX = 201328593, // HV00C (fdw_invalid_option_index)
		FDW_INVALID_OPTION_NAME = 218105809, // HV00D (fdw_invalid_option_name)
		FDW_INVALID_STRING_LENGTH_OR_BUFFER_LENGTH = 2361297, // HV090 (fdw_invalid_string_length_or_buffer_length)
		FDW_INVALID_STRING_FORMAT = 167774161, // HV00A (fdw_invalid_string_format)
		FDW_INVALID_USE_OF_NULL_POINTER = 150996945, // HV009 (fdw_invalid_use_of_null_pointer)
		FDW_TOO_MANY_HANDLES = 67373009, // HV014 (fdw_too_many_handles)
		FDW_OUT_OF_MEMORY = 16779217, // HV001 (fdw_out_of_memory)
		FDW_NO_SCHEMAS = 419432401, // HV00P (fdw_no_schemas)
		FDW_OPTION_NAME_NOT_FOUND = 318769105, // HV00J (fdw_option_name_not_found)
		FDW_REPLY_HANDLE = 335546321, // HV00K (fdw_reply_handle)
		FDW_SCHEMA_NOT_FOUND = 436209617, // HV00Q (fdw_schema_not_found)
		FDW_TABLE_NOT_FOUND = 452986833, // HV00R (fdw_table_not_found)
		FDW_UNABLE_TO_CREATE_EXECUTION = 352323537, // HV00L (fdw_unable_to_create_execution)
		FDW_UNABLE_TO_CREATE_REPLY = 369100753, // HV00M (fdw_unable_to_create_reply)
		FDW_UNABLE_TO_ESTABLISH_CONNECTION = 385877969, // HV00N (fdw_unable_to_establish_connection)
		#endregion

		#region Class P0 - PL/pgSQL Error
		PLPGSQL_ERROR = 25, // P0000 (plpgsql_error)
		RAISE_EXCEPTION = 16777241, // P0001 (raise_exception)
		NO_DATA_FOUND = 33554457, // P0002 (no_data_found)
		TOO_MANY_ROWS = 50331673, // P0003 (too_many_rows)
		ASSERT_FAILURE = 67108889, // P0004 (assert_failure)
		#endregion

		#region Class XX - Internal Error
		INTERNAL_ERROR = 2145, // XX000 (internal_error)
		DATA_CORRUPTED = 16779361, // XX001 (data_corrupted)
		INDEX_CORRUPTED = 33556577, // XX002 (index_corrupted)
		#endregion
	}


	/// <summary>
	/// mode bitmask for lo_open
	/// </summary>
	[Flags]
	public enum LoOpen
	{
		INV_WRITE = 0x00020000,
		INV_READ = 0x00040000
	};


	/// <remarks>https://msdn.microsoft.com/en-us/library/system.security.suppressunmanagedcodesecurityattribute%28v=vs.100%29.aspx</remarks>
	[SuppressUnmanagedCodeSecurity]
	internal static partial class UnsafeNativeMethods
	{
		/// <summary>
		/// wraps C functions from libpq.dll
		/// </summary>
		internal static class PqsqlWrapper
		{
			// libpq.dll depends on libeay32.dll, libintl-8.dll, ssleay32.dll
			// (DllImport would throw a DllNotFoundException if some of them are missing)
			// Note: On Windows, there is a way to improve performance if a single database connection is repeatedly started and shutdown. Internally, libpq calls WSAStartup() and WSACleanup() for connection startup and shutdown, respectively. WSAStartup() increments an internal Windows library reference count which is decremented by WSACleanup(). When the reference count is just one, calling WSACleanup() frees all resources and all DLLs are unloaded. This is an expensive operation. To avoid this, an application can manually call WSAStartup() so resources will not be freed when the last database connection is closed.

			#region PQExpBuffer

			[DllImport("libpq")]
			public static extern IntPtr createPQExpBuffer();
			// PQExpBuffer createPQExpBuffer(void);

			[DllImport("libpq")]
			public static extern void destroyPQExpBuffer(IntPtr s);
			// void destroyPQExpBuffer(PQExpBuffer str);

			[DllImport("libpq")]
			public static extern void resetPQExpBuffer(IntPtr s);
			// void resetPQExpBuffer(PQExpBuffer str);

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-misc.html
			//

			#region libpq setup

			[DllImport("libpq")]
			public static extern int PQlibVersion();
			//int PQlibVersion(void)

			[DllImport("libpq")]
			public static extern int PQisthreadsafe();
			//int PQisthreadsafe(void)

			#endregion

			#region Miscellaneous

			[DllImport("libpq")]
			public static extern void PQfreemem(IntPtr ptr);
			// void PQfreemem(void *ptr); 

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-connect.html
			//

			#region blocking connection setup

			[DllImport("libpq", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
			public static extern IntPtr PQconnectdb([MarshalAs(UnmanagedType.LPStr)] string conninfo);
			// PGconn *PQconnectdb(const char *conninfo)

			[DllImport("libpq", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
			public static extern IntPtr PQconnectdbParams([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] keywords, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] values, int expand_dbname);
			// PGconn *PQconnectdbParams(const char * const *keywords, const char * const *values, int expand_dbname);

			[DllImport("libpq")]
			public static extern void PQreset(IntPtr conn);
			// void PQreset(PGconn *conn);

			#endregion

			#region non-blocking connection setup

			[DllImport("libpq", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
			public static extern IntPtr PQconnectStartParams([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] keywords, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] values, int expand_dbname);
			// PGconn *PQconnectStartParams(const char * const *keywords, const char * const *values, int expand_dbname);

			[DllImport("libpq", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
			public static extern IntPtr PQconnectStart([MarshalAs(UnmanagedType.LPStr)] string conninfo);
			// PGconn *PQconnectStart(const char *conninfo);

			[DllImport("libpq")]
			public static extern int PQconnectPoll(IntPtr conn);
			// PostgresPollingStatusType PQconnectPoll(PGconn *conn);

			[DllImport("libpq")]
			public static extern int PQsocket(IntPtr conn);
			// int PQsocket(const PGconn *conn);

			#endregion

			#region connection settings

			[DllImport("libpq")]
			public static extern int PQsetSingleRowMode(IntPtr conn);
			// int PQsetSingleRowMode(PGconn *conn);

			[DllImport("libpq")]
			public static extern int PQclientEncoding(IntPtr conn);
			// int PQclientEncoding(const PGconn *conn);

			[DllImport("libpq")]
			public static extern int PQsetClientEncoding(IntPtr conn, byte[] encoding);
			// int PQsetClientEncoding(PGconn* conn, const char* encoding);

			#endregion

			#region connection cleanup

			[DllImport("libpq")]
			public static extern void PQfinish(IntPtr conn);
			// void PQfinish(PGconn *conn)

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-status.html
			//

			#region connection status and error message

			[DllImport("libpq")]
			public static extern ConnStatusType PQstatus(IntPtr conn);
			// ConnStatusType PQstatus(conn)

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQerrorMessage(IntPtr conn);
			// char *PQerrorMessage(const PGconn *conn);

			#endregion

			#region transaction status

			[DllImport("libpq")]
			public static extern PGTransactionStatusType PQtransactionStatus(IntPtr conn);
			// PGTransactionStatusType PQtransactionStatus(const PGconn *conn);

			#endregion

			#region connection settings

			[DllImport("libpq")]
			public static extern int PQbackendPID(IntPtr conn);
			// int PQbackendPID(const PGconn *conn);

			[DllImport("libpq")]
			public static extern int PQserverVersion(IntPtr conn);
			// int PQserverVersion(const PGconn *conn);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQparameterStatus(IntPtr conn, byte[] paramName);
			// const char *PQparameterStatus(const PGconn *conn, const char *paramName);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQdb(IntPtr conn);
			// char *PQdb(const PGconn *conn);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQhost(IntPtr conn);
			// char *PQhost(const PGconn *conn);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQport(IntPtr conn);
			// char *PQport(const PGconn *conn);

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-exec.html
			//

			#region blocking queries

			[DllImport("libpq")]
			public static extern unsafe IntPtr PQexec(IntPtr conn, byte* query);
			// PGresult *PQexec(PGconn *conn, const char *query);

			[DllImport("libpq")]
			public static extern unsafe int PQexecParams(IntPtr conn, byte* command, int nParams, IntPtr paramTypes, IntPtr paramValues, IntPtr paramLengths, IntPtr paramFormats, int resultFormat);
			// PGresult *PQexecParams(PGconn *conn, const char *command, int nParams, const Oid *paramTypes, const char * const *paramValues, const int *paramLengths, const int *paramFormats, int resultFormat);

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-async.html
			//

			#region non-blocking queries

			[DllImport("libpq")]
			public static extern unsafe int PQsendQuery(IntPtr conn, byte* query);
			// int PQsendQuery(PGconn *conn, const char *command);

			[DllImport("libpq")]
			public static extern unsafe int PQsendQueryParams(IntPtr conn, byte* command, int nParams, IntPtr paramTypes, IntPtr paramValues, IntPtr paramLengths, IntPtr paramFormats, int resultFormat);
			// int PQsendQueryParams(PGconn *conn, const char *command, int nParams, const Oid *paramTypes, const char * const *paramValues, const int *paramLengths, const int *paramFormats, int resultFormat);

			[DllImport("libpq")]
			public static extern IntPtr PQgetResult(IntPtr conn);
			// PGresult *PQgetResult(PGconn *conn)

			[DllImport("libpq")]
			public static extern int PQconsumeInput(IntPtr conn);
			// int PQconsumeInput(PGconn *conn);

			[DllImport("libpq")]
			public static extern int PQisBusy(IntPtr conn);
			// int PQisBusy(PGconn *conn);

			#endregion

			#region result cleanup

			[DllImport("libpq")]
			public static extern void PQclear(IntPtr res);
			// void PQclear(PGresult *res);

			#endregion

			#region number of rows and columns

			[DllImport("libpq")]
			public static extern int PQntuples(IntPtr res);
			// int PQntuples(const PGresult *res);

			[DllImport("libpq")]
			public static extern int PQnfields(IntPtr res);
			// int PQnfields(const PGresult *res);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQcmdTuples(IntPtr res);
			// char* PQcmdTuples(PGresult* res);

			#endregion

			#region field type and size information

			[DllImport("libpq")]
			public static extern int PQfformat(IntPtr res, int column_number);
			// int PQfformat(const PGresult *res, int column_number);

			[DllImport("libpq")]
			public static extern int PQftype(IntPtr res, int column_number);
			// Oid PQftype(const PGresult *res, int column_number);

			[DllImport("libpq")]
			public static extern int PQfmod(IntPtr res, int column_number);
			// int PQfmod(const PGresult *res, int column_number);

			[DllImport("libpq")]
			public static extern int PQfsize(IntPtr res, int column_number);
			// int PQfsize(const PGresult *res, int column_number);

			[DllImport("libpq", CharSet=CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
			public static extern int PQfnumber(IntPtr res, [MarshalAs(UnmanagedType.LPStr)] string column_name);
			// int PQfnumber(const PGresult *res, const char *column_name);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQfname(IntPtr res, int column_number);
			// char *PQfname(const PGresult *res, int column_number);

			[DllImport("libpq")]
			public static extern uint PQftable(IntPtr res, int column_number);
			// Oid PQftable(const PGresult *res, int column_number);

			#endregion

			#region value access of specified row,column

			[DllImport("libpq")]
			public static extern IntPtr PQgetvalue(IntPtr res, int row_number, int column_number);
			// char *PQgetvalue(const PGresult *res, int row_number, int column_number);

			[DllImport("libpq")]
			public static extern int PQgetisnull(IntPtr res, int row_number, int column_number);
			// int PQgetisnull(const PGresult *res, int row_number, int column_number);

			[DllImport("libpq")]
			public static extern int PQgetlength(IntPtr res, int row_number, int column_number);
			// int PQgetlength(const PGresult *res, int row_number, int column_number);

			#endregion

			#region result status and error message

			[DllImport("libpq")]
			public static extern ExecStatusType PQresultStatus(IntPtr res);
			// ExecStatusType PQresultStatus(const PGresult *res);

			[DllImport("libpq")]
			public static extern unsafe sbyte* PQresultErrorField(IntPtr res, int fieldcode);
			// char *PQresultErrorField(const PGresult *res, int fieldcode);

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-cancel.html
			//

			#region cancel query

			[DllImport("libpq")]
			public static extern IntPtr PQgetCancel(IntPtr conn);
			// PGcancel* PQgetCancel(PGconn* conn);

			[DllImport("libpq")]
			public static extern void PQfreeCancel(IntPtr cancel);
			// void PQfreeCancel(PGcancel* cancel);

			[DllImport("libpq")]
			public static extern unsafe int PQcancel(IntPtr cancel, sbyte* errbuf, int errbufsize);
			// int PQcancel(PGcancel *cancel, char *errbuf, int errbufsize);

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/libpq-copy.html
			//

			#region COPY FROM STDIN

			[DllImport("libpq")]
			public static extern int PQputCopyData(IntPtr conn, IntPtr buffer, int nbytes);
			// int PQputCopyData(PGconn *conn, const char *buffer, int nbytes);

			[DllImport("libpq")]
			public static extern unsafe int PQputCopyEnd(IntPtr conn, byte* errormsg);
			// int PQputCopyEnd(PGconn *conn, const char *errormsg);

			#endregion

			#region COPY TO STDOUT

			[DllImport("libpq")]
			public static unsafe extern int PQgetCopyData(IntPtr conn, IntPtr buffer, int async);
			// int PQgetCopyData(PGconn* conn, char** buffer, int async);

			#endregion

			//
			// http://www.postgresql.org/docs/current/static/largeobjects.html
			//

			#region LO creat / unlink

			[DllImport("libpq")]
			public static extern uint lo_creat(IntPtr conn, int mode);
			// Oid lo_creat(PGconn* conn, int mode);

			[DllImport("libpq")]
			public static extern uint lo_create(IntPtr conn, uint lobjId);
			// Oid lo_create(PGconn *conn, Oid lobjId);

			[DllImport("libpq")]
			public static extern int lo_unlink(IntPtr conn, uint lobjId);
			// int lo_unlink(PGconn *conn, Oid lobjId);

			#endregion

			#region LO open / close

			[DllImport("libpq")]
			public static extern int lo_open(IntPtr conn, uint lobjId, int mode);
			// int lo_open(PGconn *conn, Oid lobjId, int mode);

			[DllImport("libpq")]
			public static extern int lo_close(IntPtr conn, int fd);
			// int lo_close(PGconn *conn, int fd);

			#endregion

			#region LO lseek / tell

			[DllImport("libpq")]
			public static extern int lo_lseek(IntPtr conn, int fd, int offset, int whence);
			// int lo_lseek(PGconn* conn, int fd, int offset, int whence);

			[DllImport("libpq")]
			public static extern long lo_lseek64(IntPtr conn, int fd, long offset, int whence);
			// pg_int64 lo_lseek64(PGconn *conn, int fd, pg_int64 offset, int whence);

			[DllImport("libpq")]
			public static extern int lo_tell(IntPtr conn, int fd);
			// int lo_tell(PGconn *conn, int fd);

			[DllImport("libpq")]
			public static extern long lo_tell64(IntPtr conn, int fd);
			// pg_int64 lo_tell64(PGconn *conn, int fd);

			#endregion

			#region LO write / read / truncate

			[DllImport("libpq")]
			public static extern unsafe int lo_write(IntPtr conn, int fd, byte* buf, ulong len);
			// int lo_write(PGconn *conn, int fd, const char *buf, size_t len);

			[DllImport("libpq")]
			public static extern unsafe int lo_read(IntPtr conn, int fd, byte* buf, ulong len);
			// int lo_read(PGconn *conn, int fd, char *buf, size_t len);

			[DllImport("libpq")]
			public static extern int lo_truncate(IntPtr conn, int fd, long len);
			// int lo_truncate(PGcon *conn, int fd, size_t len);

			[DllImport("libpq")]
			public static extern int lo_truncate64(IntPtr conn, int fd, long len);
			// int lo_truncate64(PGcon *conn, int fd, pg_int64 len);

			#endregion
		}

	}
}
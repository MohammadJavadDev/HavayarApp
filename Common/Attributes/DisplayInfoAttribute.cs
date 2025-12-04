using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DisplayInfoAttribute(string? searchPath, bool addToTable = false 
        , SystemType type = SystemType.String , bool systemProprty = false , bool showInRelationData = false 
        ,string fileTypes ="" , int maxFileSize = 10, bool required = false , string? regex =null
        , string? regexInvalidError ="کاراکتر وارد شده غیر مجاز میباشد. " , long start = 1 , long step =1) : Attribute
    {
     
        public bool AddToTable { get; } = addToTable;
        public string? SearchPath { get; } = searchPath;
        //string bool decimal int long select enum entity file  datetime date time autoNumber
        public SystemType type { get; } = type;
        public bool SystemProprty { get; } = systemProprty;
        public bool ShowInRelationData { get; } = showInRelationData;
        public string FileTypes { get; } = fileTypes;

        public long Start { get; } = start;
        public long Step { get; } = step;

        //*.png *.jpg *.jpge
        //*.docm *.docx *.dot *.dotx
        //*.pdf

        public int MaxFileSize { get; } = maxFileSize;
        // MB
        public bool Required { get; } = required;
        public string? Regex { get; } = regex;
        // ^[\\u0600-\\u06FF ]+$ just persion
        //^[A-Za-z][A-Za-z0-9]*$ lattin
        //^[\u06F0-\u06F90-9]+$ numbers
        public string RegexInvalidError { get; } = regexInvalidError;
    }
     public enum SystemType
     {
          String,
          Boolean,
          DateTime,
          Date,
          DateTimeShamsi,
          DateShamsi,
          Long,
          Int,
          Select,
          File,
          Entity,
          ListEntity,
          ListString,
          ListLong,
		Decimal,
		AutoNumber
	}
}

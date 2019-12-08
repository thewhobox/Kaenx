using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Context.Catalog
{
    public class AppParameter
    {
        [Key]
        public string Id { get; set; }
        [MaxLength(100)]
        public string ParameterTypeId { get; set; }
        [MaxLength(100)]
        public string ApplicationId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }

        [MaxLength(100)]
        public string AbsoluteSegmentId { get; set; }
        public int Offset { get; set; }
        public int OffsetBit { get; set; }
        public AccessType Access { get; set; }

        public void LoadPara(AppParameter para)
        {
            Id = para.Id;
            ParameterTypeId = para.ParameterTypeId;
            ApplicationId = para.ApplicationId;
            Text = para.Text;
            AbsoluteSegmentId = para.AbsoluteSegmentId;
            Offset = para.Offset;
            OffsetBit = para.OffsetBit;
            Access = para.Access;
            Value = para.Value;
        }
    }

    public enum AccessType
    {
        Null,
        Full,
        None,
        Read
    }
}

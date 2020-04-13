using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppParameter
    {
        [Key]
        [MaxLength(255)]
        public string Id { get; set; }
        [MaxLength(100)]
        public string ParameterTypeId { get; set; }
        [MaxLength(100)]
        public string ApplicationId { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }

        [MaxLength(100)]
        public string SegmentId { get; set; }
        public SegmentTypes SegmentType { get; set; }
        public int Offset { get; set; }
        public int OffsetBit { get; set; }
        public AccessType Access { get; set; }

        public void LoadPara(AppParameter para)
        {
            Id = para.Id;
            ParameterTypeId = para.ParameterTypeId;
            ApplicationId = para.ApplicationId;
            Text = para.Text;
            SegmentType = para.SegmentType;
            SegmentId = para.SegmentId;
            Offset = para.Offset;
            OffsetBit = para.OffsetBit;
            Access = para.Access;
            Value = para.Value;
        }
    }

    public enum SegmentTypes
    {
        None = 0,
        Memory = 1,
        Property = 2
    }

    public enum AccessType
    {
        Null,
        Full,
        None,
        Read
    }
}

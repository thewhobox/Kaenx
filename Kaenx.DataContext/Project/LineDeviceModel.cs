using System.ComponentModel.DataAnnotations;

namespace Kaenx.DataContext.Project
{
    public class LineDeviceModel
    {
        [Key]
        public int UId { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string ApplicationId { get; set; }
        public bool LoadedGA { get; set; }
        public bool LoadedApp { get; set; }
        public bool LoadedPA { get; set; }
        public byte[] Serial { get; set; }

        public LineDeviceModel() { }
        public LineDeviceModel(int projId) { ProjectId = projId; }
    }
}

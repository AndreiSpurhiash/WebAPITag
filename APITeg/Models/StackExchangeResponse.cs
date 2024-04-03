using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace APITag.Models
{
    public class StackExchangeResponse
    {
        public List<Tag> items { get; set; }
    }

    public class Tag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int count { get; set; }

        public string name { get; set; }

        public double? percentage { get; set; }
    }
}
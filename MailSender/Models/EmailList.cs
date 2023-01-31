using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailSender.Models;

[Table("email_notif")]
public class EmailList
{
    [Key]
    [Column("pk")]
    public Guid Pk { get; set; }

    [Column("subject")]
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; }

    [Column("body")]
    [Required]
    [Unicode(false)]
    public string Body { get; set; }

    [Column("receivers")]
    [Required]
    [Unicode(false)]
    public string Receivers { get; set; }

    [Column("receiverscc")]
    [Unicode(false)]
    public string ReceiversCC { get; set; }

    [Column("receiversbcc")]
    [Unicode(false)]
    public string ReceiversBCC { get; set; }

    [Column("proccess_count")]
    public int ProccessCount { get; set; }

    [Column("proccess_date")]
    public DateTime? ProccessDate { get; set; }

    [Column("is_success")]
    public bool IsSuccess { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public string CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("mimetype")]
    public string? Mimetype { get; set; }

    [Column("extension")]
    public string? Extension { get; set; }

    [Column("filebase64")]
    public string? Filebase64 { get; set; }

    [Column("filename")]
    public string? Filename { get; set; }
}

[Table("email_notif_file")]
public class EmailListFile
{
    [Key]
    [Column("pk")]
    public Guid Pk { get; set; }

    [Column("subject")]
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; }

    [Column("body")]
    [Required]
    [Unicode(false)]
    public string Body { get; set; }

    [Column("receivers")]
    [Required]
    [Unicode(false)]
    public string Receivers { get; set; }

    [Column("receiverscc")]
    [Unicode(false)]
    public string? ReceiversCC { get; set; }

    [Column("receiversbcc")]
    [Unicode(false)]
    public string? ReceiversBCC { get; set; }

    [Column("proccess_count")]
    public int ProccessCount { get; set; }

    [Column("proccess_date")]
    public DateTime? ProccessDate { get; set; }

    [Column("is_success")]
    public bool IsSuccess { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public string CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("mimetype")]
    public string? Mimetype { get; set; }

    [Column("extension")]
    public string? Extension { get; set; }

    [Column("filebase64")]
    public string? Filebase64 { get; set; }

    [Column("filenametext")]
    public string? FilenameTxt { get; set; }
}

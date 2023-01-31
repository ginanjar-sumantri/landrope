using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace landrope.mod2
{
	public enum ChangeKind
	{
		Unchanged = 0,
		Add = 1,
		Update = 2,
		Delete = 3
	}

	public class ValidatableItem : DetailBase
	{
		public virtual object extras { get; set; }
	}

	public class Validation<T>
	{
		public bool reviewed;
		public bool approved;
		public string note;
		public string[] corrections;

		public Validation()
		{
			reviewed = true;
			approved = true;
			note = "";
			corrections = new string[0];
		}

		public Validation(ValidatableEntry<T> entry)
		{
			this.reviewed = entry.reviewed != null;
			approved = reviewed && !entry.isRejected();
			note = approved ? "" : entry.rejectNote;
			corrections = getCorrections();
		}
		private string[] getCorrections()
		{
			if (string.IsNullOrWhiteSpace(note))
				return new string[0];
			var parts = note.Split(new[] { "##>" }, StringSplitOptions.RemoveEmptyEntries);
			var elems = parts.Any() ? parts[0] : "";
			note = parts.Length > 1 ? parts[1] : "";
			return elems.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => "#" + e.Trim()).ToArray();
		}
	}

	public class ValidatableEntry
	{
		public ChangeKind en_kind { get; set; }
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false)]
		public DateTime? created { get; set; }
		public string keyCreator { get; set; }
		[BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = false)]
		public DateTime? reviewed { get; set; }
		public string keyReviewer { get; set; }
		public bool? approved { get; set; }
		public string rejectNote { get; set; }
		public DateTime? preReviewed { get; set; }
		public string keyPreReviewer { get; set; }
		public bool? preApproved { get; set; }
        public string preRejectNote { get; set; }
        public virtual object Item { get; }

		public ValidatableEntry MakeCopy()
		{
			var New = new ValidatableEntry();
			(New.en_kind, New.created, New.keyCreator, New.reviewed, New.keyReviewer, New.approved, New.rejectNote) =
			(en_kind, created, keyCreator, reviewed, keyReviewer, approved, rejectNote);
			return New;
		}

		public ValidatableEntry<T> MakeCopy<T>(bool withItem = false)
		{
			var New = new ValidatableEntry<T>();
			(New.en_kind, New.created, New.keyCreator, New.reviewed, New.keyReviewer, New.approved, New.rejectNote) =
			(en_kind, created, keyCreator, reviewed, keyReviewer, approved, rejectNote);
			if (withItem)
				New.item = (T)Item;
			return New;
		}
	}

	public class ValidatableEntry<T> : ValidatableEntry
	{
		public T item { get; set; }

		public Validation<T> Validation => new Validation<T>(this);

		public bool isValidated() => reviewed != null && keyReviewer != null && keyReviewer != "BCAB674C-45E4-492B-8EDE-791C872DCC15";
		public bool isRejected() => isValidated() && (approved == false);

		internal bool isValidating() => reviewed == null && keyReviewer == null;

		public override object Item => this.item; 
	}

	public class ValidatableShell
	{
		public virtual bool isExists() => false;
		public virtual bool isImported() => false;
		public virtual bool isValidated() => false;
		public virtual bool isValidating() => false;
		public virtual bool isRejected() => false;
		public virtual bool isDirty() => false;
		public virtual void PreValidate(string keyUser, bool approved, string rejectNote)
		{
		}
		public virtual void Validate(string keyUser, bool approved, string rejectNote)
		{
		}
		public virtual void NewValidate(string keyUser, bool approved, string rejectNote)
		{
		}
	}

	public class ValidatableShell<T> : ValidatableShell
	{
		public T current { get; set; }
		public List<ValidatableEntry<T>> entries { get; set; } = new List<ValidatableEntry<T>>();

		public void PutEntry(T obj, ChangeKind kind, string keyUser)
		{
			var entry = new ValidatableEntry<T> { en_kind = kind, created = DateTime.Now, keyCreator = keyUser, item = obj };
			entries.Add(entry);
		}
		public ValidatableEntry<T> GetWaiting()
		{
			var ent = entries.LastOrDefault();
			return (ent?.reviewed != null) ? null : ent;
		}

		public ValidatableEntry<T> GetLast(bool notvalidated)
		{
			return notvalidated ? entries.OrderBy(e => e.created).LastOrDefault() :
														entries.Where(e => e.reviewed == null || e.keyReviewer == null)
														.OrderBy(e => e.created).LastOrDefault();
		}

		public ValidatableEntry<T> GetLast2(bool valid) =>
			valid ? GetLastValid() : GetLastDirty();

		public ValidatableEntry<T> GetLastDirty() =>
			entries.Where(e => e.reviewed == null || e.keyReviewer == null)
			.OrderBy(e => e.created).LastOrDefault();

		public ValidatableEntry<T> GetLastValid() =>
			entries.Where(e => e.reviewed != null && e.keyReviewer != null && e.approved==true)
			.OrderBy(e => e.created).LastOrDefault();

		public ValidatableEntry<T>[] GetHistories() => entries.ToArray();

		public override void Validate(string keyUser, bool approved, string rejectNote)
		{
			if (!approved && string.IsNullOrWhiteSpace(rejectNote))
				throw new InvalidOperationException("Reject note should not be empty for rejection");
			if (!entries.Any())
				return;
			entries.ForEach(e =>
			{
				if (e.reviewed == null)
				{
					e.reviewed = DateTime.Now;
					e.keyReviewer = keyUser;
					e.approved = approved;
					e.rejectNote = rejectNote;
				}
			});

			if (approved)
			{
				var json = JsonConvert.SerializeObject(entries.Last().item);
				current = JsonConvert.DeserializeObject<T>(json);
			}
		}

		public override void NewValidate(string keyUser, bool approved, string rejectNote)
		{
			if (!approved && string.IsNullOrWhiteSpace(rejectNote))
				throw new InvalidOperationException("Reject note should not be empty for rejection");
			if (!entries.Any())
				return;
			entries.ForEach(e =>
			{
				if (e.reviewed == null)
				{
					e.reviewed = !approved?null:DateTime.Now;
					e.keyReviewer = keyUser;
					e.approved = !approved?null:approved;
					e.rejectNote = rejectNote;
				}
			});

			if (approved)
			{
				var json = JsonConvert.SerializeObject(entries.Last().item);
				current = JsonConvert.DeserializeObject<T>(json);
			}
		}

		public override void PreValidate(string keyUser, bool approved, string rejectNote)
		{
			if (!approved && string.IsNullOrWhiteSpace(rejectNote))
				throw new InvalidOperationException("Reject note not should not be empty for rejection");
			if (!entries.Any())
				return;
			entries.ForEach(e =>
			{
				if (e.reviewed == null)
				{
					e.preReviewed = DateTime.Now;
					e.keyPreReviewer = keyUser;
					e.preApproved = approved;
					e.rejectNote = rejectNote;
				}
			});
		}

		public override bool isExists() => current != null;
		public override bool isValidated() => entries.LastOrDefault()?.isValidated() ?? true;
		public override bool isValidating() => entries.LastOrDefault()?.isValidating() ?? false;
		public override bool isRejected() => entries.LastOrDefault()?.isRejected() ?? false;
		public override bool isDirty() => entries.Any(e => e.reviewed == null || e.keyReviewer == null);
		public override bool isImported()
		{
			if (current == null || entries.Count != 1 || entries[0].keyCreator != "BCAB674C-45E4-492B-8EDE-791C872DCC15")
				return false;
			var lst = ((DetailBase)(object)entries[0].item).FindChanges((DetailBase)(object)current);
			return !lst.Any();
		}
	}

}

using auth.mod;
using landrope.common;
using landrope.mod2;
using landrope.mod3.shared;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace landrope.mod3
{
	[ProtoContract(Name = "Expense_")]
	public class Expense
	{
		[ProtoMember(1)]
		public string key { get; set; }
		[ProtoMember(2)]
		public ExpensePost en_post { get; set; }

		[ProtoIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public string post
		{
			get => en_post.ToString("g");
			set { if (Enum.TryParse<ExpensePost>(value, out ExpensePost et)) en_post = et; }
		}

		[ProtoMember(3)]
		[BsonRequired]
		public ExpenseType en_type { get; set; }

		[ProtoIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public string type
		{
			get => en_type.ToString("g");
			set { if (Enum.TryParse<ExpenseType>(value, out ExpenseType et)) en_type = et; }
		}

		[ProtoMember(4)]
		landrope.mod2.PaymentRinci payment { get; set; }

		[ProtoMember(5)]
		[System.Text.Json.Serialization.JsonIgnore]
		public ExpenseValidation[] validations { get; set; } = new ExpenseValidation[0];

		public void Validate(string keyUser, bool approved, string note)
		{
			if (string.IsNullOrWhiteSpace(keyUser))
				throw new InvalidOperationException("User should be provided");
			if (!approved && string.IsNullOrWhiteSpace(note))
				throw new InvalidOperationException("Rejection note should not empty");
			var lst = validations.ToList();
			lst.Add(new ExpenseValidation { date = DateTime.Now, approved = approved, keyUser = keyUser, note = note });
			validations = lst.ToArray();
		}

		public void AddValidation(ExpenseValidationCore vcore)
		{
			var val = new ExpenseValidation();
			val.FromCore(vcore);
			var lst = validations.ToList();
			lst.Add(val);
			validations = lst.ToArray();
		}

		public object ToCore()
		{
			var core = new ExpenseCore();
			(core.key, core.en_type, core.en_post) = (key, en_type, en_post);
			return core;
		}

		public void FromCore(ExpenseCore core)
		{
			(key, en_type, en_post) = (core.key, core.en_type, core.en_post);
		}
	}

	[ProtoContract(Name = "ExpenseValidation_")]
	public class ExpenseValidation
	{
		[ProtoMember(1)]
		public DateTime date { get; set; }
		[ProtoMember(2)]
		public string keyUser { get; set; }
		[ProtoMember(3)]
		public bool approved { get; set; }
		[ProtoMember(4)]
		public string note { get; set; }

		public ExpenseValidationCore ToCore(string key)
		{
			var vcore = new ExpenseValidationCore();
			(vcore.key, vcore.date, vcore.keyUser, vcore.approved, vcore.note) =
				(key, date, keyUser, approved, note);
			return vcore;
		}

		public void FromCore(ExpenseValidationCore vcore)
		{
			(date, keyUser, approved, note) =
			(vcore.date, vcore.keyUser, vcore.approved, vcore.note);
		}
	}

}

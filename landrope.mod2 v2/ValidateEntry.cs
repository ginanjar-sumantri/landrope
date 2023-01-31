using auth.mod;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace landrope.mod2
{
	public class ValidateEntry
	{
		public ChangeKind kind { get; set; } = ChangeKind.Unchanged;
		public DateTime? created { get; set; }
		public string keyCreator { get; set; }
		public string sourceFile { get; set; }

		public DateTime? reviewed { get; set; }
		public string keyReviewer { get; set; }
		public bool? approved { get; set; }
		public string rejectNote { get; set; }

		public static ValidateEntry Copy(ValidateEntry other)
		{
			var New = new ValidateEntry();
			(New.kind, New.created, New.keyCreator, New.sourceFile, New.reviewed, New.keyReviewer, New.approved, New.rejectNote) =
			(other.kind, other.created, other.keyCreator, other.sourceFile, other.reviewed, other.keyReviewer, other.approved, other.rejectNote);
			return New;
		}
	}

	public class ValidateEntry<T> : ValidateEntry where T : class
	{
		public T Item { get; set; }
		public virtual T MakeItemCopy() => null;

		public static ValidateEntry<T> Copy(ValidateEntry<T> other, bool withItem = false)
		{
			var New = new ValidateEntry<T>();
			(New.kind, New.created, New.keyCreator, New.sourceFile, New.reviewed, New.keyReviewer, New.approved, New.rejectNote) =
			(other.kind, other.created, other.keyCreator, other.sourceFile, other.reviewed, other.keyReviewer, other.approved, other.rejectNote);
			if (withItem)
				New.Item = other.MakeItemCopy();
			return New;
		}
	}

	public enum ValidateAction
	{
		Replace,
		Add,
		Remove,
		Approval,
		Rejection
	}
	public class ValidateTerm
	{
		public ValidateAction action { get; set; }
		public ValidateTerm(ValidateAction action)
		{
			this.action = action;
		}
	}

	public class ValidateTerm<T> : ValidateTerm where T : class
	{
		public Validates<T> item { get; set; }

		public ValidateTerm(ValidateAction action, Validates<T> item)
			: base(action)
		{
			this.item = item;
		}
	}

	public class Validates<T> /*: IObservable<ValidateTerm<Validates<T>>>,IDisposable*/ where T : class
	{
		public T current { get; set; }

		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public ValidateEntry<T>[] entries { get; set; } = new ValidateEntry<T>[0];

		public void Add(ValidateEntry<T> entry)
		{
			entries = entries.Add(entry);
			/*			if (observer != null)
							observer.OnNext(new ValidateTerm<Validates<T>>(ValidateAction.Add, this));*/
		}
		public bool Del(ValidateEntry<T> entry)
		{
			if (entries == null)
				return false;
			var ent = entries.Del(entry);
			bool OK = ent?.Length != entries.Length;
			entries = ent;
			return OK;
		}

		/*		IObserver<ValidateTerm<Validates<T>>> observer;
				public IDisposable Subscribe(IObserver<ValidateTerm<Validates<T>>> observer)
				{
					this.observer = observer;
					return this;
				}
		*/
		public void Validate(user user, bool approved, string rejectNote)
		{
			if (user != null)
			{
				Validate(user.key, approved, rejectNote);
				/*				if (observer!=null)
									observer.OnNext(new ValidateTerm<Validates<T>>(approved? ValidateAction.Approval:ValidateAction.Rejection, this));*/
			}
		}

		public void Validate(string userkey, bool approved, string rejectNote)
		{
			if (entries.Length == 0 || entries.Last().reviewed != null)
				return;
			var lasts = entries.Where(e => e.reviewed == null).ToArray();
			if (approved)
				rejectNote = null;
			foreach (var ent in lasts)
				(ent.reviewed, ent.keyReviewer, ent.approved, ent.rejectNote) =
					(DateTime.Now, userkey, approved, rejectNote);
			if (approved)
				WhenValidated();
			/*			if (observer != null)
							observer.OnNext(new ValidateTerm<Validates<T>>(approved ? ValidateAction.Approval : ValidateAction.Rejection, this));*/
		}

		protected virtual void WhenValidated() { }

		/*		public void Dispose()
				{
				}*/
	}

	public class ValidateEventArgs<T> : EventArgs where T : class
	{
		public Validates<T> item { get; set; }
	}

	public class ValidateObserver<T> : IObserver<ValidateTerm<T>> where T : class
	{
		public event EventHandler<ValidateEventArgs<T>> OnAdd;
		public event EventHandler<ValidateEventArgs<T>> OnRemove;
		public event EventHandler<ValidateEventArgs<T>> OnApprove;
		public event EventHandler<ValidateEventArgs<T>> OnReject;

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(ValidateTerm<T> value)
		{
			var args = new ValidateEventArgs<T> { item = value.item };
			switch (value.action)
			{
				case ValidateAction.Add when OnAdd.GetInvocationList().Any(): OnAdd.Invoke(value, args); break;
				case ValidateAction.Remove when OnRemove.GetInvocationList().Any(): OnRemove.Invoke(value, args); break;
				case ValidateAction.Approval when OnApprove.GetInvocationList().Any(): OnApprove.Invoke(value, args); break;
				case ValidateAction.Rejection when OnReject.GetInvocationList().Any(): OnReject.Invoke(value, args); break;
			}
		}
	}
}

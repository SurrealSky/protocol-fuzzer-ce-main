using System;
using System.Diagnostics;
using NLog;

namespace Peach.Core.Dom
{
	[Serializable]
	[DebuggerDisplay("From={FromName} Of={OfName}")]
	public class Binding
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private DataElement of;

		public Binding(DataElement parent)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");

			From = parent;
			FromName = parent.Name;

			parent.relations.Add(this);
		}

		public void Clear()
		{
			if (of != null)
			{
				bool removed = of.relations.Remove(this);
				System.Diagnostics.Debug.Assert(removed);
				of.Invalidated -= OfInvalidated;
				of = null;

				// When Of is lost, From needs to be invalidated
				From.Invalidate();

				OnClear();
			}
		}

		public void Resolve()
		{
			// Optimistic resolution called by PitParser, don't log if binding fails...
			Resolve(false);
		}

		private void Resolve(bool log)
		{
			if (of == null && OfName != null)
			{
				var elem = From.find(OfName);

				if (elem == null)
				{
					if (log)
						logger.Debug("Unable to resolve binding '" + OfName + "' attached to '" + From.fullName + "'.");
				}
				else if (From.CommonParent(elem) is Choice)
				{
					logger.Error("Binding '" + OfName + "' attached to '" + From.fullName + "' cannot share a common parent that is of type 'Choice'.");
				}
				else
				{
					SetOf(elem);
				}
			}
		}

		protected virtual void OnResolve()
		{
		}

		protected virtual void OnClear()
		{
		}

		/// <summary>
		/// The DataElement that owns the binding.
		/// </summary>
		public DataElement From { get; private set; }

		/// <summary>
		/// The name of the DataElement that owns the binding.
		/// </summary>
		public string FromName { get; private set; }

		/// <summary>
		/// The name of the DataElement on the remote side of the binding.
		/// </summary>
		public string OfName { get; set; }

		/// <summary>
		/// The DataElement on the remote side of the binding.
		/// </summary>
		[DebuggerDisplay("{OfDebugName}")]
		public DataElement Of
		{
			get
			{
				// Resolution needed because someone asked for Of, log if not found...
				if (of == null)
					Resolve(true);

				return of;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (From.CommonParent(value) is Choice)
					throw new ArgumentException("Binding '" + value.fullName + "' attached to '" + From.fullName + "' cannot share a common parent that is of type 'Choice'.");

				Clear();

				OfName = value.Name;

				SetOf(value);
			}
		}

		private string OfDebugName
		{
			get
			{
				return of != null ? of.debugName : null;
			}
		}

		private void SetOf(DataElement elem)
		{
			of = elem;
			of.Invalidated += new InvalidatedEventHandler(OfInvalidated);
			of.relations.Add(this);

			From.Invalidate();

			OnResolve();
		}

		private void OfInvalidated(object sender, EventArgs e)
		{
			From.Invalidate();
		}

		[OnCloned]
		private void OnCloned(Binding original, object context)
		{
			// DataElement.Invalidated is not serialized, so register for a re-subscribe to the event
			if (of != null)
				of.Invalidated += new InvalidatedEventHandler(OfInvalidated);

			DataElement.CloneContext ctx = context as DataElement.CloneContext;

			if (ctx != null)
			{
				// If 'From' or 'Of' was renamed, ensure the name is correct
				FromName = ctx.UpdateRefName(original.From, original.From, FromName);
				OfName = ctx.UpdateRefName(original.From, original.of, OfName);

				// If this 'From' is the same as the original,
				// then the data element was not a child of the data element
				// that was cloned.  This binding needs to be added to the
				// original 'From' element.
				if (From == original.From)
				{
					System.Diagnostics.Debug.Assert(!original.From.relations.Contains(this));
					From.relations.Add(this);
				}

				// If this 'Of' is the same as the original,
				// then the data element was not a child of the data element
				// that was cloned.  This binding needs to be added to the
				// original 'Of' element.
				if (of != null && of == original.of)
				{
					System.Diagnostics.Debug.Assert(!original.of.relations.Contains(this));
					of.relations.Add(this);
				}
			}
		}
	}
}

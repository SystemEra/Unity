using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using FullInspector;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class fiSettingsModifier
{
	static fiSettingsModifier()
	{
		bool modifiedJsonDefault = false;
		try
		{
			if (FullInspector.Internal.fiInstalledSerializerManager.DefaultMetadata is FullInspector.Serializers.ModifiedJsonNet.ModifiedJsonNetMetadata)
				modifiedJsonDefault = true;
		}
		catch { }
		
		if (!modifiedJsonDefault)
			FullInspector.Internal.fiDefaultSerializerRewriter.GenerateFileChangeDefault("FullInspector.Serializers.ModifiedJsonNet.ModifiedJsonNetMetadata", "FullInspector.ModifiedJsonNetSerializer");

		FullInspector.fiSettings.InspectorShowAutoProperties = false;
		FullInspector.fiMetadataCallbacks.ListMetadataCallback = AddListMetadata;
		FullInspector.fiMetadataCallbacks.PropertyMetadataCallback = AddPropertyMetadata;
	}

	private static void AddListMetadata(fiGraphMetadata metadata, IList list, int index)
	{
		fiMemberTraversalMetadata memberItemMetadata;
		if (!metadata.TryGetMetadata<fiMemberTraversalMetadata>(out memberItemMetadata))
		{
			fiMemberTraversalMetadata currentMemberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
			memberItemMetadata = metadata.GetMetadata<fiMemberTraversalMetadata>();
			if (currentMemberItem != null)
				memberItemMetadata.NestedMemberTraversal = new fiNestedMemberTraversal(currentMemberItem.NestedMemberTraversal, new fiMemberTraversal(o => (o as IList)[index]));
			else
				memberItemMetadata.NestedMemberTraversal = new fiNestedMemberTraversal(new fiMemberTraversal(o => (o as IList)[index]));
		}
	}

	private static void AddArrayMetadata(fiGraphMetadata metadata, Array list, int index)
	{
		fiMemberTraversalMetadata memberItemMetadata;
		if (!metadata.TryGetMetadata<fiMemberTraversalMetadata>(out memberItemMetadata))
		{
			fiMemberTraversalMetadata currentMemberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
			memberItemMetadata = metadata.GetMetadata<fiMemberTraversalMetadata>();
			if (currentMemberItem != null)
				memberItemMetadata.NestedMemberTraversal = new fiNestedMemberTraversal(currentMemberItem.NestedMemberTraversal, new fiMemberTraversal(o => (o as IList)[index]));
			else
				memberItemMetadata.NestedMemberTraversal = new fiNestedMemberTraversal(new fiMemberTraversal(o => (o as IList)[index]));
		}
	}

	private static void AddPropertyMetadata(fiGraphMetadata metadata, InspectedProperty property)
	{
		fiMemberTraversalMetadata memberItemMetadata;
		if (!metadata.TryGetMetadata<fiMemberTraversalMetadata>(out memberItemMetadata))
		{
			fiMemberTraversalMetadata currentMemberItem = metadata.GetInheritedMetadata<fiMemberTraversalMetadata>();
			memberItemMetadata = metadata.GetMetadata<fiMemberTraversalMetadata>();
			if (currentMemberItem != null)
				memberItemMetadata.NestedMemberTraversal = new fiNestedMemberTraversal(currentMemberItem.NestedMemberTraversal, new fiMemberTraversal(o => property.Read(o)));
			else
				memberItemMetadata.NestedMemberTraversal = new fiNestedMemberTraversal(new fiMemberTraversal(o => property.Read(o)));

			memberItemMetadata.DeclaringType = property.MemberInfo.DeclaringType;
			memberItemMetadata.MemberInfo = property.MemberInfo;
			memberItemMetadata.StorageType = property.StorageType;
		}

		var scopeAttrs = property.MemberInfo.GetCustomAttributes(typeof(ScopeAttribute), true);
		if (scopeAttrs.Any())
		{
			metadata.GetMetadata<ViewScopeMetadata>().ViewType = (scopeAttrs.First() as ScopeAttribute).View;
		}

		var defaultFolderAttrs = property.MemberInfo.GetCustomAttributes(typeof(DefaultFolderAttribute), true);
		if (defaultFolderAttrs.Any())
		{
			metadata.GetMetadata<DefaultFolderMetadata>().Folder = (defaultFolderAttrs.First() as DefaultFolderAttribute).Folder;
		}
	}
}
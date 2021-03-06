﻿//-----------------------------------------------------------------------
// <copyright file="CloudFileDirectory.Common.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.File.Protocol;
    using System;

    /// <summary>
    /// Represents a directory of files, designated by a delimiter character.
    /// </summary>
    /// <remarks>Shares, which are encapsulated as <see cref="CloudFileShare"/> objects, hold directories, and directories hold files. Directories can also contain sub-directories.</remarks>
    public sealed partial class CloudFileDirectory : IListFileItem
    {
        /// <summary>
        /// Stores the <see cref="CloudFileShare"/> that contains this directory.
        /// </summary>
        private CloudFileShare share;

        /// <summary>
        /// Stores the parent directory.
        /// </summary>
        private CloudFileDirectory parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class using an absolute URI to the directory.
        /// </summary>
        /// <param name="directoryAbsoluteUri">The absolute URI to the directory.</param>
        /// <param name="credentials">The account credentials.</param>
        public CloudFileDirectory(Uri directoryAbsoluteUri, StorageCredentials credentials)
            : this(new StorageUri(directoryAbsoluteUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class using an absolute URI to the directory.
        /// </summary>
        /// <param name="directoryAbsoluteUri">The absolute URI to the directory.</param>
        /// <param name="credentials">The account credentials.</param>
#if WINDOWS_RT
        /// <returns>A <see cref="CloudFileDirectory"/> object.</returns>
        public static CloudFileDirectory Create(StorageUri directoryAbsoluteUri, StorageCredentials credentials)
        {
            return new CloudFileDirectory(directoryAbsoluteUri, credentials);
        }

        internal CloudFileDirectory(StorageUri directoryAbsoluteUri, StorageCredentials credentials)
#else
        public CloudFileDirectory(StorageUri directoryAbsoluteUri, StorageCredentials credentials)
#endif
        {
            this.Properties = new FileDirectoryProperties();
            this.ParseQueryAndVerify(directoryAbsoluteUri, credentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFileDirectory"/> class given an address and a client.
        /// </summary>
        /// <param name="uri">The file directory's Uri.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <param name="share">The share for the directory.</param>
        internal CloudFileDirectory(StorageUri uri, string directoryName, CloudFileShare share)
        {
            CommonUtility.AssertNotNull("uri", uri);
            CommonUtility.AssertNotNull("directoryName", directoryName);
            CommonUtility.AssertNotNull("share", share);

            this.Properties = new FileDirectoryProperties();
            this.StorageUri = uri;
            this.ServiceClient = share.ServiceClient;
            this.share = share;
            this.Name = directoryName;
        }

        /// <summary>
        /// Gets a <see cref="CloudFileClient"/> object that represents the service client for the directory.
        /// </summary>
        /// <value>A <see cref="CloudFileClient"/> object that specifies the endpoint for the File service.</value>
        public CloudFileClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the URI that identifies the directory for the primary location.
        /// </summary>
        /// <value>The URI to the directory, at the primary location.</value>
        public Uri Uri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the file directory's URIs for all locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing the file directory's URIs for all locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets a <see cref="FileDirectoryProperties"/> object that represents the directory's system properties.
        /// </summary>
        /// <value>A <see cref="FileDirectoryProperties"/> object containing the directory's properties.</value>
        public FileDirectoryProperties Properties { get; internal set; }

        /// <summary>
        /// Gets a <see cref="CloudFileShare"/> object that represents the share for the directory.
        /// </summary>
        /// <value>The share for the directory.</value>
        public CloudFileShare Share
        {
            get
            {
                if (this.share == null)
                {
                    this.share = this.ServiceClient.GetShareReference(
                        NavigationHelper.GetShareName(this.Uri, this.ServiceClient.UsePathStyleUris));
                }

                return this.share;
            }
        }

        /// <summary>
        /// Gets a <see cref="CloudFileDirectory"/> object that represents the parent directory for the directory.
        /// </summary>
        /// <value>The directory's parent directory.</value>
        public CloudFileDirectory Parent
        {
            get
            {
                if (this.parent == null)
                {
                    string parentName;
                    StorageUri parentUri;
                    if (NavigationHelper.GetFileParentNameAndAddress(this.StorageUri, this.ServiceClient.UsePathStyleUris, out parentName, out parentUri))
                    {
                        this.parent = new CloudFileDirectory(parentUri, parentName, this.Share);
                    }
                }

                return this.parent;
            }
        }

        /// <summary>
        /// Gets the name of the directory.
        /// </summary>
        /// <value>The name of the directory.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Selects the protocol response.
        /// </summary>
        /// <param name="protocolItem">The protocol item.</param>
        /// <returns>The parsed <see cref="IListFileItem"/>.</returns>
        private IListFileItem SelectListFileItem(IListFileEntry protocolItem)
        {
            ListFileEntry file = protocolItem as ListFileEntry;
            if (file != null)
            {
                CloudFileAttributes attributes = file.Attributes;
                attributes.StorageUri = NavigationHelper.AppendPathToUri(this.StorageUri, file.Name);
                return new CloudFile(attributes, this.ServiceClient);
            }

            ListFileDirectoryEntry fileDirectory = protocolItem as ListFileDirectoryEntry;
            if (fileDirectory != null)
            {
                CloudFileDirectory directory = this.GetDirectoryReference(fileDirectory.Name);
                directory.Properties = fileDirectory.Properties;
                return directory;
            }

            throw new InvalidOperationException(SR.InvalidFileListItem);
        }

        /// <summary>
        /// Returns a <see cref="CloudFile"/> object that represents a file in this directory.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>A <see cref="CloudFile"/> object representing a reference to a file.</returns>
        public CloudFile GetFileReference(string fileName)
        {
            CommonUtility.AssertNotNullOrEmpty("fileName", fileName);

            StorageUri subdirectoryUri = NavigationHelper.AppendPathToUri(this.StorageUri, fileName);
            return new CloudFile(subdirectoryUri, fileName, this.Share);
        }

        /// <summary>
        /// Returns a <see cref="CloudFileDirectory"/> object that represents a subdirectory within this directory.
        /// </summary>
        /// <param name="itemName">The name of the subdirectory.</param>
        /// <returns>A <see cref="CloudFileDirectory"/> object representing the subdirectory.</returns>
        public CloudFileDirectory GetDirectoryReference(string itemName)
        {
            CommonUtility.AssertNotNullOrEmpty("itemName", itemName);

            StorageUri subdirectoryUri = NavigationHelper.AppendPathToUri(this.StorageUri, itemName);
            return new CloudFileDirectory(subdirectoryUri, itemName, this.Share);
        }

        /// <summary>
        /// Parse URI.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
        {
            this.StorageUri = address;
            this.ServiceClient = new CloudFileClient(NavigationHelper.GetServiceClientBaseAddress(this.StorageUri, null /* usePathStyleUris */), credentials);
            this.Name = NavigationHelper.GetFileName(this.Uri, this.ServiceClient.UsePathStyleUris);
        }
    }
}

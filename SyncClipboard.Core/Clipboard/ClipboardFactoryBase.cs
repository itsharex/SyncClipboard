﻿using Microsoft.Extensions.DependencyInjection;
using SyncClipboard.Abstract;
using SyncClipboard.Core.Interfaces;
using SyncClipboard.Core.Models;
using SyncClipboard.Core.Utilities.Image;
using System.Net;
using System.Text.Json;

namespace SyncClipboard.Core.Clipboard;

public abstract class ClipboardFactoryBase : IClipboardFactory, IProfileDtoHelper
{
    protected abstract ILogger Logger { get; set; }
    protected abstract IServiceProvider ServiceProvider { get; set; }
    protected abstract IWebDav WebDav { get; set; }

    public abstract ClipboardMetaInfomation GetMetaInfomation();

    private string RemoteProfilePath => ServiceProvider.GetRequiredService<IAppConfig>().RemoteProfilePath;

    public Profile CreateProfile(ClipboardMetaInfomation? metaInfomation = null)
    {
        metaInfomation ??= GetMetaInfomation();

        if (metaInfomation.Files != null)
        {
            var filename = metaInfomation.Files[0];
            if (File.Exists(filename))
            {
                if (ImageHelper.FileIsImage(filename))
                {
                    return new ImageProfile(filename, ServiceProvider);
                }
                return new FileProfile(filename, ServiceProvider);
            }
        }

        if (metaInfomation.Text != null)
        {
            return new TextProfile(metaInfomation.Text, ServiceProvider);
        }

        if (metaInfomation.Image != null)
        {
            return new ImageProfile(metaInfomation.Image, ServiceProvider);
        }

        return new UnkonwnProfile();
    }

    public async Task<Profile> CreateProfileFromRemote(CancellationToken cancelToken)
    {
        ClipboardProfileDTO? profileDTO;
        try
        {
            profileDTO = await WebDav.GetJson<ClipboardProfileDTO>(RemoteProfilePath, cancelToken);
            Logger.Write(nameof(ClipboardFactoryBase), profileDTO?.ToString() ?? "null");
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound })
            {
                var blankProfile = new TextProfile("", ServiceProvider);
                await blankProfile.UploadProfile(WebDav, cancelToken);
                return blankProfile;
            }
            Logger.Write("CreateFromRemote failed");
            throw;
        }

        ArgumentNullException.ThrowIfNull(profileDTO);
        ProfileType type = ProfileTypeHelper.StringToProfileType(profileDTO.Type);
        return GetProfileBy(type, profileDTO);
    }

    private Profile GetProfileBy(ProfileType type, ClipboardProfileDTO profileDTO)
    {
        switch (type)
        {
            case ProfileType.Text:
                return new TextProfile(profileDTO.Clipboard, ServiceProvider);
            case ProfileType.File:
                {
                    if (ImageHelper.FileIsImage(profileDTO.File))
                    {
                        return new ImageProfile(profileDTO, ServiceProvider);
                    }
                    return new FileProfile(profileDTO, ServiceProvider);
                }
            case ProfileType.Image:
                return new ImageProfile(profileDTO, ServiceProvider);
        }

        return new UnkonwnProfile();
    }

    public string CreateProfileDto(out string? extraFilePath)
    {
        extraFilePath = null;
        var profile = CreateProfile();
        if (profile is FileProfile fileProfile)
        {
            extraFilePath = fileProfile.FullPath;
        }
        return profile.ToJsonString();
    }

    public void SetLocalClipboardWithDto(string profileDtoString, string fileFolder)
    {
        var profileDTO = JsonSerializer.Deserialize<ClipboardProfileDTO>(profileDtoString);
        ArgumentNullException.ThrowIfNull(profileDTO);
        ProfileType type = ProfileTypeHelper.StringToProfileType(profileDTO.Type);
        var profile = GetProfileBy(type, profileDTO);
        if (profile is FileProfile fileProfile)
        {
            fileProfile.FullPath = Path.Combine(fileFolder, fileProfile.FileName);
        }

        profile.SetLocalClipboard(true);
    }
}
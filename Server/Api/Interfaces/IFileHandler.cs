﻿namespace Api.Interfaces
{
    public interface IFileHandler
    {
        public Task<string?> Upload(IFormFile file, string path, string fileName);
        public string Download();
    }
}

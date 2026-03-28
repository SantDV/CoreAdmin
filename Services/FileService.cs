using System.Windows.Forms;

namespace CoreAdmin.Services;

public class FileService
{
    public async Task<string?> SaveFileAsync(string fileName, byte[] content)
    {
        using var sfd = new SaveFileDialog
        {
            FileName = fileName,
            Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            await File.WriteAllBytesAsync(sfd.FileName, content);
            return sfd.FileName;
        }

        return null;
    }
}

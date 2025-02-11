using GalgameManager.Models;
using GalgameManager.Models.BgTasks;

namespace GalgameManager.Contracts.Phrase;

public interface IGalStaffPhraser
{
    /// <summary>
    /// 获取staff信息，直接修改传入的staff对象 <br/>
    /// 注意捕获异常
    /// </summary>
    /// <param name="staff"></param>
    /// <returns></returns>
    public Task<Staff> GetStaffAsync(Staff staff);

    /// <summary>
    /// 获取该游戏的所有staff信息（简略版），待后续进一步解析（如果设置中打开了解析游戏制作人的选项）<br/>
    /// 如果获取不到则返回空列表 <br/>
    /// <b>内部不捕获异常，调用方需捕获异常</b>
    /// </summary>
    /// <exception cref="HttpRequestException"></exception>
    /// <returns></returns>
    public Task<List<StaffRelation>> GetStaffsAsync(Galgame game);
}
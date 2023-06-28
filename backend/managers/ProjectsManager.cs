using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ProjectsManager : BaseCosmos
{
    public async Task<Project> UpdateProject(Project project)
    {
        return await UpdateItem(project, projectsContainer, true);
    }

    public async Task<Project> GetProject(string projectId)
    {
        return await ReadItem<Project>(projectId, projectsContainer, projectId);
    }

    public async Task<Project> GetProjectByCompanyId(string companyId)
    {
        string sql = "select * from c where c.companyId = @companyId";
        var projects = await ExecuteQuery<Project>(sql, projectsContainer, new Dictionary<string, object>()
        {
            { "@companyId", companyId }
        });

        return projects.FirstOrDefault();
    }
}
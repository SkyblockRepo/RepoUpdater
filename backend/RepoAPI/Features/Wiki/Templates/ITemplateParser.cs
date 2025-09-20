namespace RepoAPI.Features.Wiki.Templates;

// A generic interface for any template parser.
// T is the DTO type the parser will return.
public interface ITemplateParser<out T> where T : class
{
	/// <summary>
	/// Parses raw wikitext from a template into a structured DTO.
	/// </summary>
	/// <param name="wikitext">The raw wikitext content of the template.</param>
	/// <param name="backupId">Backup internal id</param>
	/// <returns>A populated DTO of type T.</returns>
	T Parse(string wikitext, string backupId = "");
	
	/// <summary>
	/// Gets the wikitext template for a given input string.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	string GetTemplate(string input);
}
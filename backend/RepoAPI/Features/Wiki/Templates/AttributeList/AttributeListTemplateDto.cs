namespace RepoAPI.Features.Wiki.Templates.AttributeList;

public class AttributeListTemplateDto
{
	public List<AttributeTemplateDto> Attributes { get; set; } = [];
}

public class AttributeTemplateDto
{
	public required string ShardName { get; set; }
	public required string Rarity { get; set; }
	public int Id { get; set; }
	public string? Category { get; set; }
	public string? Family { get; set; }
	public AttributeEffectDto? AttributeEffect { get; set; } 
	public string? Description { get; set; }
	public string? Obtaining { get; set; }
	public string? FusionInput1 { get; set; }
	public string? FusionInput2 { get; set; }
	public string? FusionResult { get; set; }
	public string? FusionOrigin { get; set; }
}

public class StatBonusDto
{
	public required string Description { get; set; }
	public required string FromValue { get; set; }
	public required string ToValue { get; set; }
}

public class AttributeEffectDto
{
	public required string Name { get; set; }
	public List<StatBonusDto> Bonuses { get; set; } = [];
}
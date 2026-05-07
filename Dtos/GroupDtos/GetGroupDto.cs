namespace ParrotsAPI2.Dtos.GroupDtos
{
    public class GetGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public DateTime? LastMessageDate { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new();
    }
}

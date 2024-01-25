
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Data;

namespace ParrotsAPI2.Services.Character
{
    public class CharacterService : ICharacterService
    {

        //private static List<Character> characters = new List<Character>
        //{
        //    new Character(),
        //    new Character { Id=1 , Name = "Sam"}

        //};

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public CharacterService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }


        public async Task<ServiceResponse<List<GetCharacterDto>>> AddCharacter(AddCharacterDto newCharacter)
        {
            var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();
            var character = _mapper.Map<Models.Character>(newCharacter);
            _context.Characters.Add(character);
            await _context.SaveChangesAsync();
            var updatedCharacters = await _context.Characters.ToListAsync();
            serviceResponse.Data = updatedCharacters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetCharacterDto>>> DeleteCharacter(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();
            try
            {
                var character = await _context.Characters.FindAsync(id);

                if (character == null)
                {
                    throw new Exception($"Character with ID `{id}` not found");
                }

                _context.Characters.Remove(character);
                await _context.SaveChangesAsync();
                var updatedCharacters = await _context.Characters.ToListAsync();
                serviceResponse.Data = updatedCharacters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetCharacterDto>>> GetAllCharacters()
        {
            var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();
            var dbCharacters = await _context.Characters.ToListAsync();
            serviceResponse.Data = dbCharacters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> GetCharacterById(int id)
        {
            var serviceResponse = new ServiceResponse<GetCharacterDto>();
            var dbCharacters = await _context.Characters.ToListAsync();
            var character = dbCharacters.FirstOrDefault(c => c.Id == id);
            serviceResponse.Data = _mapper.Map<GetCharacterDto>(character);
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> UpdateCharacter(UpdateCharacterDto updatedCharacter)
        {
            var serviceResponse = new ServiceResponse<GetCharacterDto>();
            try
            {
                var character = await _context.Characters.FindAsync(updatedCharacter.Id);

                if (character == null)
                {
                    throw new Exception($"Character with ID `{updatedCharacter.Id}` not found");
                }

                character.Name = updatedCharacter.Name;
                character.Hitpoints = updatedCharacter.Hitpoints;
                character.Defense = updatedCharacter.Defense;
                character.Intelligence = updatedCharacter.Intelligence;
                character.Class = updatedCharacter.Class;

                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetCharacterDto>(character);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }


    }
}


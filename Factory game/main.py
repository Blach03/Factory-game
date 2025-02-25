import pygame
import time

pygame.init()

GRID_SIZE = 20
SQUARE_SIZE = 30
WIDTH, HEIGHT = GRID_SIZE * SQUARE_SIZE, GRID_SIZE * SQUARE_SIZE + 100
MENU_HEIGHT = 100
FPS = 60

WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
RED = (255, 0, 0)
BLUE = (0, 0, 255)
GREEN = (0, 255, 0)
PINK = (255, 105, 180)
GRAY = (200, 200, 200)
DARK_GRAY = (150, 150, 150)
ORANGE = (255, 165, 0)

conveyor_img = pygame.image.load("conveyor.png")

screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption("20x20 Grid Game")


class Item:
    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.radius = 5

    def draw(self, surface):
        pygame.draw.circle(surface, BLACK, (self.x, self.y), self.radius)

class GridSquare:
    def __init__(self, x, y, size):
        self.rect = pygame.Rect(x, y, size, size)
        self.occupied = None

    def draw(self, surface):
        if self.occupied:
            if self.occupied.image:
                rotated_image = pygame.transform.rotate(self.occupied.image, self.occupied.rotation)
                surface.blit(rotated_image, self.rect.topleft)
            else:
                pygame.draw.rect(surface, self.occupied.color, self.rect)
        else:
            pygame.draw.rect(surface, WHITE, self.rect)
        pygame.draw.rect(surface, BLACK, self.rect, 1)


class Block:
    def __init__(self, tiles, color=None, image=None, rotation=0):
        self.tiles = tiles
        self.color = color
        self.image = image
        self.rotation = rotation

    def rotate(self):
        self.rotation = (self.rotation - 90) % 360

    def draw(self, surface):
        for tile in self.tiles:
            if self.image:
                rotated_image = pygame.transform.rotate(self.image, self.rotation)
                surface.blit(rotated_image, tile.rect.topleft)
            else:
                pygame.draw.rect(surface, self.color, tile.rect)
            pygame.draw.rect(surface, BLACK, tile.rect, 1)

class OrangeBlock(Block):
    def __init__(self, tiles):
        super().__init__(tiles, ORANGE)
        self.last_generated = time.time()
        self.items = []

    def generate_item(self):
        current_time = time.time()
        if current_time - self.last_generated >= 3:
            print("generating")
            main_tile = self.tiles[4] 
            grid_x, grid_y = main_tile.rect.x // SQUARE_SIZE, (main_tile.rect.y // SQUARE_SIZE) + 2
            if grid_y < GRID_SIZE:  
                item_x = grid[grid_y][grid_x].rect.centerx
                item_y = grid[grid_y][grid_x].rect.centery
                self.items.append(Item(item_x, item_y))
                self.last_generated = current_time  


    def draw(self, surface):
        super().draw(surface)
        self.generate_item()
        for item in self.items:
            item.draw(surface)

class MenuItem:
    def __init__(self, x, y, width, height, color=None, block_type=None, image=None):
        self.rect = pygame.Rect(x, y, width, height)
        self.color = color
        self.block_type = block_type
        self.selected = False
        self.image = image
        self.rotation = 0  

    def rotate(self):
        self.rotation = (self.rotation - 90) % 360

    def draw(self, surface):
        bg_color = GRAY if self.selected else BLACK
        border_color = RED if self.selected else WHITE
        pygame.draw.rect(surface, bg_color, self.rect)
        if self.image:
            rotated_image = pygame.transform.rotate(self.image, self.rotation)
            surface.blit(rotated_image, self.rect.topleft)
        else:
            pygame.draw.rect(surface, self.color, self.rect.inflate(-5, -5))
        pygame.draw.rect(surface, border_color, self.rect, 3)

grid = [[GridSquare(x * SQUARE_SIZE, y * SQUARE_SIZE, SQUARE_SIZE) for x in range(GRID_SIZE)] for y in range(GRID_SIZE)]
blocks = []

menu_items = [
    MenuItem(10, HEIGHT - 80, 60, 60, RED, "2x2_red"),
    MenuItem(80, HEIGHT - 80, 60, 60, BLUE, "2x2_blue"),
    MenuItem(150, HEIGHT - 80, 60, 60, GREEN, "2x2_green"),
    MenuItem(220, HEIGHT - 80, 30, 30, None, "1x1_black", conveyor_img),
    MenuItem(260, HEIGHT - 90, 30, 90, PINK, "1x3_pink"),
    MenuItem(320, HEIGHT - 80, 60, 60, ORANGE, "3x3_orange"),
    MenuItem(390, HEIGHT - 80, 60, 60, WHITE, "remove")
]

selected_item = None
hovered_tiles = []
mousedown = False
last_deselected_time = 0
last_selected_time = 0
running = True
clock = pygame.time.Clock()
while running:
    screen.fill(BLACK)
    
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False
        elif event.type == pygame.MOUSEBUTTONDOWN:
            mousedown = True
        elif event.type == pygame.MOUSEBUTTONUP:
            mousedown = False
        elif event.type == pygame.KEYDOWN:
            if event.key == pygame.K_r and selected_item:
                selected_item.rotate()
        
    mx, my = pygame.mouse.get_pos()
    if mousedown:
        for item in menu_items:
            if item.rect.collidepoint(mx, my):
                if selected_item == item:
                    if pygame.time.get_ticks() - last_selected_time > 200:
                        selected_item.selected = False
                        selected_item = None
                        last_deselected_time = pygame.time.get_ticks()
                else:
                    for i in menu_items:
                        i.selected = False
                    if pygame.time.get_ticks() - last_deselected_time > 200:
                        item.selected = True
                        selected_item = item
                        last_selected_time = pygame.time.get_ticks()
                break
        else:
            if selected_item and selected_item.block_type == "remove":
                for block in blocks:
                    if any(tile.rect.collidepoint(mx, my) for tile in block.tiles):
                        for tile in block.tiles:
                            tile.occupied = None
                        blocks.remove(block)
                        break
            elif hovered_tiles and selected_item and all(tile.occupied is None for tile in hovered_tiles):
                if selected_item.block_type == "3x3_orange":
                    new_block = OrangeBlock(hovered_tiles)
                    for tile in hovered_tiles:
                        tile.occupied = new_block
                    blocks.append(new_block)
                else:
                    new_block = Block(hovered_tiles, selected_item.color, selected_item.image, selected_item.rotation)
                    for tile in hovered_tiles:
                        tile.occupied = new_block
                    blocks.append(new_block)
                
    hovered_tiles = []
    grid_x, grid_y = mx // SQUARE_SIZE, my // SQUARE_SIZE
    if 0 <= grid_x < GRID_SIZE and 0 <= grid_y < GRID_SIZE and selected_item:
        if selected_item.block_type in ["2x2_red", "2x2_blue", "2x2_green"]:
            if grid_x + 1 < GRID_SIZE and grid_y + 1 < GRID_SIZE:
                hovered_tiles = [grid[grid_y + dy][grid_x + dx] for dy in range(2) for dx in range(2)]
        elif selected_item.block_type == "1x1_black":
            hovered_tiles = [grid[grid_y][grid_x]]
        elif selected_item.block_type == "1x3_pink":
            if grid_y + 2 < GRID_SIZE:
                hovered_tiles = [grid[grid_y + dy][grid_x] for dy in range(3)]
        elif selected_item.block_type == "3x3_orange":
            if grid_x + 2 < GRID_SIZE and grid_y + 2 < GRID_SIZE:
                hovered_tiles = [grid[grid_y + dy][grid_x + dx] for dy in range(3) for dx in range(3)]
    for row in grid:
        for square in row:
            square.draw(screen)
    
    for block in blocks:
        block.draw(screen)

    for block in blocks:
        if isinstance(block, OrangeBlock):
            for item in block.items:
                item.draw(screen)

    
    for tile in hovered_tiles:
        pygame.draw.rect(screen, GRAY, tile.rect)
    
    for item in menu_items:
        item.draw(screen)
    
    pygame.display.flip()
    clock.tick(FPS)
    

pygame.quit()
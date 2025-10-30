from PIL import Image, ImageDraw
import math

def create_storage_icon(size, output_path):
    """
    Create a custom icon showing boxes on shelves with light red circular background
    """
    # Create image with transparent background
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Draw off-white circle background
    off_white = (245, 245, 240, 255)  # Soft off-white color
    draw.ellipse([0, 0, size, size], fill=off_white)
    
    # Colors
    brown = (139, 69, 19, 255)  # Box color
    dark_brown = (101, 50, 14, 255)  # Shelf color (darker)
    box_outline = (80, 40, 10, 255)  # Darker outline
    
    # Calculate dimensions (work with relative sizes)
    margin = size * 0.15
    content_width = size - (2 * margin)
    content_height = size - (2 * margin)
    
    # Shelf properties
    shelf_thickness = size * 0.04
    num_shelves = 3
    shelf_spacing = content_height / (num_shelves + 1)
    
    # Box dimensions
    box_width = size * 0.12
    box_height = size * 0.10  # Reduced height so boxes don't overlap shelves
    box_depth = size * 0.06  # For 3D effect
    
    # Draw shelves with 3D effect
    for i in range(num_shelves):
        shelf_y = margin + shelf_spacing * (i + 1)
        shelf_depth = box_depth  # Match the depth of the boxes for consistency
        
        # Front face of shelf
        draw.rectangle(
            [margin - size * 0.05, shelf_y, 
             margin + content_width + size * 0.05, shelf_y + shelf_thickness],
            fill=dark_brown,
            outline=box_outline,
            width=max(1, int(size * 0.005))
        )
        
        # Top face of shelf (visible from above)
        shelf_left = margin - size * 0.05
        shelf_right = margin + content_width + size * 0.05
        top_points = [
            (shelf_left, shelf_y),
            (shelf_left + shelf_depth * 0.5, shelf_y - shelf_depth * 0.3),
            (shelf_right + shelf_depth * 0.5, shelf_y - shelf_depth * 0.3),
            (shelf_right, shelf_y)
        ]
        draw.polygon(top_points, fill=(121, 70, 34, 255), outline=box_outline)  # Lighter brown for top
        
        # Right edge of shelf
        right_points = [
            (shelf_right, shelf_y),
            (shelf_right + shelf_depth * 0.5, shelf_y - shelf_depth * 0.3),
            (shelf_right + shelf_depth * 0.5, shelf_y + shelf_thickness - shelf_depth * 0.3),
            (shelf_right, shelf_y + shelf_thickness)
        ]
        draw.polygon(right_points, fill=(81, 50, 24, 255), outline=box_outline)  # Darker brown for side
    
    # Draw boxes on shelves
    # Shelf 1 - 3 boxes
    shelf_1_y = margin + shelf_spacing * 1 - box_height
    boxes_shelf_1 = [
        margin + size * 0.05,
        margin + size * 0.22,
        margin + size * 0.39
    ]
    
    # Shelf 2 - 2 boxes
    shelf_2_y = margin + shelf_spacing * 2 - box_height
    boxes_shelf_2 = [
        margin + size * 0.1,
        margin + size * 0.32
    ]
    
    # Shelf 3 - 3 boxes
    shelf_3_y = margin + shelf_spacing * 3 - box_height
    boxes_shelf_3 = [
        margin + size * 0.02,
        margin + size * 0.2,
        margin + size * 0.42
    ]
    
    # Function to draw a 3D box (isometric view from front-top)
    def draw_3d_box(x, y, w, h, d):
        # Front face
        draw.rectangle(
            [x, y, x + w, y + h],
            fill=brown,
            outline=box_outline,
            width=max(1, int(size * 0.005))
        )
        
        # Top face (visible from above, receding backward)
        # Top face goes back (up on screen) and to the right
        top_points = [
            (x, y),
            (x + d * 0.5, y - d * 0.3),  # Back-left corner
            (x + w + d * 0.5, y - d * 0.3),  # Back-right corner
            (x + w, y)
        ]
        draw.polygon(top_points, fill=(169, 89, 39, 255), outline=box_outline)
        
        # Right face (side of box)
        right_points = [
            (x + w, y),
            (x + w + d * 0.5, y - d * 0.3),
            (x + w + d * 0.5, y + h - d * 0.3),
            (x + w, y + h)
        ]
        draw.polygon(right_points, fill=(109, 59, 29, 255), outline=box_outline)
        
        # Add simple box detail (horizontal line in middle representing tape/seal)
        line_y = y + h * 0.4
        draw.line(
            [(x + w * 0.2, line_y), (x + w * 0.8, line_y)],
            fill=box_outline,
            width=max(1, int(size * 0.004))
        )
    
    # Draw all boxes
    for box_x in boxes_shelf_1:
        draw_3d_box(box_x, shelf_1_y, box_width, box_height, box_depth)
    
    for box_x in boxes_shelf_2:
        draw_3d_box(box_x, shelf_2_y, box_width, box_height, box_depth)
    
    for box_x in boxes_shelf_3:
        draw_3d_box(box_x, shelf_3_y, box_width, box_height, box_depth)
    
    # Save the image
    img.save(output_path, 'PNG')
    print(f"Created custom icon: {output_path}")

# Create both sizes
icons_dir = r"e:\Projects\storage-labels\storage-labels-ui\src\static\icons"

create_storage_icon(192, f"{icons_dir}/storage-container-192x192.png")
create_storage_icon(512, f"{icons_dir}/storage-container-512x512.png")

print("\nCustom storage icons created successfully!")
print("Original icons are still backed up in the backup folder.")

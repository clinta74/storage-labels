from PIL import Image, ImageDraw
import os

def update_icon(input_path, output_path):
    """
    Update icon by adding a light red circular background and coloring the box brown
    """
    # Open the original image
    img = Image.open(input_path).convert('RGBA')
    width, height = img.size
    
    # Create a new image with transparent background
    new_img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(new_img)
    
    # Draw light red circle background
    # Light red color: #FFB3BA (255, 179, 186)
    light_red = (255, 179, 186, 255)
    circle_margin = 0  # Circle fills the entire canvas
    draw.ellipse([circle_margin, circle_margin, width - circle_margin, height - circle_margin], 
                 fill=light_red)
    
    # Load the original image pixel data
    pixels = img.load()
    new_pixels = new_img.load()
    
    # Process each pixel
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            
            # Skip fully transparent pixels
            if a == 0:
                continue
            
            # Check if pixel is part of the box (darker colors, not white/light)
            # We'll color non-white, non-transparent pixels brown
            # Brown color: #8B4513 (139, 69, 19)
            if a > 0:
                # If pixel has color (not transparent), make it brown
                # Preserve some variation by mixing with original intensity
                intensity = (r + g + b) / (3 * 255)
                brown_r = int(139 * intensity)
                brown_g = int(69 * intensity)
                brown_b = int(19 * intensity)
                new_pixels[x, y] = (brown_r, brown_g, brown_b, a)
    
    # Save the updated image
    new_img.save(output_path, 'PNG')
    print(f"Updated icon saved to: {output_path}")

# Update both icon sizes
icons_dir = r"e:\Projects\storage-labels\storage-labels-ui\src\static\icons"
update_icon(
    os.path.join(icons_dir, "backup", "storage-container-192x192.png"),
    os.path.join(icons_dir, "storage-container-192x192.png")
)
update_icon(
    os.path.join(icons_dir, "backup", "storage-container-512x512.png"),
    os.path.join(icons_dir, "storage-container-512x512.png")
)

print("\nIcons updated successfully!")
print(f"Original icons backed up to: {os.path.join(icons_dir, 'backup')}")

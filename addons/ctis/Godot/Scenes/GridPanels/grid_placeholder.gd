@tool
extends Control

const TILE := 32.0
const DEFAULT_TEXTURE := preload("res://addons/ctis/Art/Sprites/UI_Inventory/BagSlot.png")

@export var width_cells: int = 1:
	set(value):
		width_cells = maxi(1, value)
		_apply_size()
		queue_redraw()

@export var height_cells: int = 1:
	set(value):
		height_cells = maxi(1, value)
		_apply_size()
		queue_redraw()

@export_group("Editor Preview")
@export var cell_texture: Texture2D = DEFAULT_TEXTURE:
	set(value):
		cell_texture = value
		queue_redraw()

@export var show_outline: bool = true:
	set(value):
		show_outline = value
		queue_redraw()

@export var outline_color: Color = Color(0.25, 0.5, 0.9, 0.7):
	set(value):
		outline_color = value
		queue_redraw()


func _enter_tree() -> void:
	_apply_size()
	queue_redraw()


func _ready() -> void:
	_apply_size()
	queue_redraw()


func _apply_size() -> void:
	var next := Vector2(width_cells * TILE, height_cells * TILE)
	custom_minimum_size = next
	size = next


func _draw() -> void:
	var tex: Texture2D = cell_texture if cell_texture != null else DEFAULT_TEXTURE
	if tex != null:
		for y in range(height_cells):
			for x in range(width_cells):
				draw_texture_rect(tex, Rect2(x * TILE, y * TILE, TILE, TILE), false)

	# Draws an editor outline for alignment and visualization.
	if Engine.is_editor_hint() and show_outline:
		draw_rect(Rect2(Vector2.ZERO, size), outline_color, false, 1.0)


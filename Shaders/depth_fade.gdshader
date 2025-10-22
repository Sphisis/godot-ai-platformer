shader_type canvas_item;

uniform sampler2D light_texture : filter_linear, repeat_disable;
uniform vec2 tilemap_offset = vec2(0.0, 0.0);
uniform vec2 tilemap_size = vec2(30.0, 18.0);
uniform int tile_size = 16;
uniform float ambient_light : hint_range(0.0, 1.0) = 0.1;
uniform float light_intensity : hint_range(0.0, 5.0) = 1.0;
uniform vec3 light_color : source_color = vec3(1.0, 1.0, 1.0);

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	
	vec2 tilemap_world_origin = tilemap_offset * float(tile_size);
	vec2 pos_in_tilemap = world_pos - tilemap_world_origin;
	vec2 texture_size_pixels = tilemap_size * float(tile_size);
	vec2 texture_uv = pos_in_tilemap / texture_size_pixels;
	
	float light = texture(light_texture, texture_uv).r;
	light = max(light, ambient_light);
	light *= light_intensity;
	
	vec3 lit_color = tex.rgb * light * light_color;
	COLOR = vec4(lit_color, tex.a);
}

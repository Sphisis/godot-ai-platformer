shader_type canvas_item;

uniform sampler2D light_texture : filter_linear, repeat_disable;
uniform vec2 tilemap_offset = vec2(0.0, 0.0);
uniform vec2 tilemap_size = vec2(30.0, 18.0);
uniform int tile_size = 16;
uniform float ambient_light : hint_range(0.0, 1.0) = 0.1;
uniform float light_intensity : hint_range(0.0, 5.0) = 1.0;
uniform vec3 light_color : source_color = vec3(1.0, 1.0, 1.0);
uniform float blur_radius : hint_range(0.0, 50.0) = 10.0;
uniform int blur_quality : hint_range(1, 3) = 2; // 1=fast, 2=balanced, 3=quality

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float sample_light_blurred(vec2 uv) {
	vec2 tex_size = vec2(textureSize(light_texture, 0));
	if (tex_size.x <= 0.0 || tex_size.y <= 0.0) {
		return texture(light_texture, uv).r;
	}
	
	vec2 texel = 1.0 / tex_size;
	
	// Adjust step size based on quality: 1=every 3rd pixel, 2=every 2nd, 3=every pixel
	int step = 4 - blur_quality;
	int radius = int(blur_radius);
	
	float accum = 0.0;
	float weight_sum = 0.0;
	float sigma = blur_radius / 3.0;
	float two_sigma_sq = 2.0 * sigma * sigma;
	
	// Optimized blur with variable step size
	for (int y = -radius; y <= radius; y += step) {
		for (int x = -radius; x <= radius; x += step) {
			vec2 offset = vec2(float(x), float(y)) * texel;
			float dist_sq = float(x * x + y * y);
			float weight = exp(-dist_sq / two_sigma_sq);
			accum += texture(light_texture, uv + offset).r * weight;
			weight_sum += weight;
		}
	}
	
	return accum / max(weight_sum, 0.0001);
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	
	vec2 tilemap_world_origin = tilemap_offset * float(tile_size);
	vec2 pos_in_tilemap = world_pos - tilemap_world_origin;
	vec2 texture_size_pixels = tilemap_size * float(tile_size);
	vec2 texture_uv = pos_in_tilemap / texture_size_pixels;
	
	float light = sample_light_blurred(texture_uv);
	light = max(light, ambient_light);
	light *= light_intensity;
	
	vec3 lit_color = tex.rgb * light * light_color;
	COLOR = vec4(lit_color, tex.a);
}
